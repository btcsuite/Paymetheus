﻿// Copyright (c) 2016 The btcsuite developers
// Licensed under the ISC license.  See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace Paymetheus.Bitcoin.Wallet
{
    public sealed class Wallet
    {
        /// <summary>
        /// The number of recent blocks which are always kept in memory in a synced wallet.
        /// Transactions from these recent blocks are used to subtract output amounts from
        /// the total balance to calculate spendable balances given some number of confirmations.
        /// This value may not be less than the coinbase maturity level, or immature coinbase
        /// outputs will not be subtracted from the spendable balance.
        /// </summary>
        public const int NumRecentBlocks = BlockChain.CoinbaseMaturity;

        /// <summary>
        /// The minimum number of transactions to always keep in memory for a synced wallet,
        /// so long as at least this many transactions are managed by the wallet process.
        /// If at least this number of transactions do not exist in the last NumRecentBlocks
        /// blocks and unmined transactions, earlier blocks and their transactions will be
        /// saved in memory.
        /// </summary>
        public const int MinRecentTransactions = 10;

        /// <summary>
        /// The length in bytes of the wallet's BIP0032 seed.
        /// </summary>
        public const int SeedLength = 32;

        public Wallet(BlockChainIdentity activeChain, TransactionSet txSet, Dictionary<Account, AccountState> accounts, BlockIdentity chainTip)
        {
            if (accounts == null)
                throw new ArgumentNullException(nameof(accounts));
            if (chainTip == null)
                throw new ArgumentNullException(nameof(chainTip));

            var totalBalance = accounts.Aggregate((Amount)0, (acc, kvp) => acc + kvp.Value.TotalBalance);

            _transactionCount = txSet.MinedTransactions.Aggregate(0, (acc, b) => acc + b.Transactions.Count) +
                txSet.UnminedTransactions.Count;
            _accounts = accounts;

            ActiveChain = activeChain;
            RecentTransactions = txSet;
            TotalBalance = totalBalance;
            ChainTip = chainTip;
        }

        private int _transactionCount;
        private readonly Dictionary<Account, AccountState> _accounts = new Dictionary<Account, AccountState>();

        public BlockChainIdentity ActiveChain { get; }
        public TransactionSet RecentTransactions { get; }
        public Amount TotalBalance { get; private set; }
        public BlockIdentity ChainTip { get; private set; }

        private void AddTransactionToTotals(WalletTransaction tx, Dictionary<Account, AccountState> modifiedAccounts)
        {
            var isCoinbase = BlockChain.IsCoinbase(tx.Transaction);

            foreach (var input in tx.Inputs)
            {
                TotalBalance -= input.Amount;

                var accountState = _accounts[input.PreviousAccount];
                accountState.TotalBalance -= input.Amount;
                if (isCoinbase)
                    accountState.ImmatureCoinbaseReward -= input.Amount;
                modifiedAccounts[input.PreviousAccount] = accountState;
            }

            foreach (var output in tx.Outputs.OfType<WalletTransaction.Output.ControlledOutput>())
            {
                TotalBalance += output.Amount;

                var accountState = _accounts[output.Account];
                accountState.TotalBalance += output.Amount;
                if (isCoinbase)
                    accountState.ImmatureCoinbaseReward += output.Amount;
                modifiedAccounts[output.Account] = accountState;
            }

            _transactionCount++;
        }

        private void RemoveTransactionFromTotals(WalletTransaction tx, Dictionary<Account, AccountState> modifiedAccounts)
        {
            var isCoinbase = BlockChain.IsCoinbase(tx.Transaction);

            foreach (var input in tx.Inputs)
            {
                TotalBalance += input.Amount;

                var accountState = _accounts[input.PreviousAccount];
                accountState.TotalBalance += input.Amount;
                if (isCoinbase)
                    accountState.ImmatureCoinbaseReward += input.Amount;
                modifiedAccounts[input.PreviousAccount] = accountState;
            }

            foreach (var output in tx.Outputs.OfType<WalletTransaction.Output.ControlledOutput>())
            {
                TotalBalance -= output.Amount;

                var accountState = _accounts[output.Account];
                accountState.TotalBalance -= output.Amount;
                if (isCoinbase)
                    accountState.ImmatureCoinbaseReward -= output.Amount;
                modifiedAccounts[output.Account] = accountState;
            }

            _transactionCount--;
        }

        public class ChangesProcessedEventArgs : EventArgs
        {
            // Update transactions, confirmations, balances.  Value is null if tip did not change.
            public BlockIdentity? NewChainTip { get; internal set; }

            // Transactions with a changed location (moved from unmined to a block,
            // block to another block, or block to unmined).
            public Dictionary<Sha256Hash, BlockIdentity> MovedTransactions { get; } = new Dictionary<Sha256Hash, BlockIdentity>();

            public List<Tuple<WalletTransaction, BlockIdentity>> AddedTransactions { get; } = new List<Tuple<WalletTransaction, BlockIdentity>>();

            public List<WalletTransaction> RemovedTransactions { get; } = new List<WalletTransaction>();

            public Dictionary<Account, AccountState> ModifiedAccountStates { get; } = new Dictionary<Account, AccountState>();
        }

        public event EventHandler<ChangesProcessedEventArgs> ChangesProcessed;

        private void OnChangesProcessed(ChangesProcessedEventArgs e)
        {
            ChangesProcessed?.Invoke(this, e);
        }

        public void ApplyTransactionChanges(WalletChanges changes)
        {
            if (changes == null)
                throw new ArgumentNullException(nameof(changes));

            // A reorganize cannot be handled if the number of removed blocks exceeds the
            // minimum number saved in memory.
            if (changes.DetachedBlocks.Count >= NumRecentBlocks)
                throw new BlockChainConsistencyException("Reorganize too deep");

            var newChainTip = changes.AttachedBlocks.LastOrDefault();
            if (ChainTip.Height >= newChainTip?.Height)
            {
                var msg = $"New chain tip {newChainTip.Hash} (height {newChainTip.Height}) neither extends nor replaces " +
                    $"the current chain (currently synced to hash {ChainTip.Hash}, height {ChainTip.Height})";
                throw new BlockChainConsistencyException(msg);
            }

            if (changes.NewUnminedTransactions.Any(tx => !changes.AllUnminedHashes.Contains(tx.Hash)))
                throw new BlockChainConsistencyException("New unmined transactions contains tx with hash not found in all unmined transaction hash set");

            var eventArgs = new ChangesProcessedEventArgs();

            var reorgedBlocks = RecentTransactions.MinedTransactions
                .ReverseList()
                .TakeWhile(b => changes.DetachedBlocks.Contains(b.Hash))
                .ToList();
            var numReorgedBlocks = reorgedBlocks.Count;
            foreach (var reorgedTx in reorgedBlocks.SelectMany(b => b.Transactions))
            {
                if (BlockChain.IsCoinbase(reorgedTx.Transaction) || !changes.AllUnminedHashes.Contains(reorgedTx.Hash))
                {
                    RemoveTransactionFromTotals(reorgedTx, eventArgs.ModifiedAccountStates);
                }
                else
                {
                    RecentTransactions.UnminedTransactions[reorgedTx.Hash] = reorgedTx;
                    eventArgs.MovedTransactions.Add(reorgedTx.Hash, BlockIdentity.Unmined);
                }
            }
            var numRemoved = RecentTransactions.MinedTransactions.RemoveAll(block => changes.DetachedBlocks.Contains(block.Hash));
            if (numRemoved != numReorgedBlocks)
            {
                throw new BlockChainConsistencyException("Number of blocks removed exceeds those for which transactions were removed");
            }

            foreach (var block in changes.AttachedBlocks.Where(b => b.Transactions.Count > 0))
            {
                RecentTransactions.MinedTransactions.Add(block);

                foreach (var tx in block.Transactions)
                {
                    if (RecentTransactions.UnminedTransactions.ContainsKey(tx.Hash))
                    {
                        RecentTransactions.UnminedTransactions.Remove(tx.Hash);
                        eventArgs.MovedTransactions[tx.Hash] = block.Identity;
                    }
                    else if (!eventArgs.MovedTransactions.ContainsKey(tx.Hash))
                    {
                        AddTransactionToTotals(tx, eventArgs.ModifiedAccountStates);
                        eventArgs.AddedTransactions.Add(Tuple.Create(tx, block.Identity));
                    }
                }
            }

            // TODO: What about new transactions which were not added in a newly processed
            // block (e.g. importing an address and rescanning for outputs)?

            foreach (var tx in changes.NewUnminedTransactions.Where(tx => !RecentTransactions.UnminedTransactions.ContainsKey(tx.Hash)))
            {
                RecentTransactions.UnminedTransactions[tx.Hash] = tx;
                AddTransactionToTotals(tx, eventArgs.ModifiedAccountStates);

                // TODO: When reorgs are handled, this will need to check whether the transaction
                // being added to the unmined collection was previously in a block.
                eventArgs.AddedTransactions.Add(Tuple.Create(tx, BlockIdentity.Unmined));
            }

            var removedUnmined = RecentTransactions.UnminedTransactions
                .Where(kvp => !changes.AllUnminedHashes.Contains(kvp.Key))
                .ToList(); // Collect to list so UnminedTransactions can be modified below.
            foreach (var unmined in removedUnmined)
            {
                // Transactions that were mined rather than being removed from the unmined
                // set due to a conflict have already been removed.
                RecentTransactions.UnminedTransactions.Remove(unmined.Key);
                RemoveTransactionFromTotals(unmined.Value, eventArgs.ModifiedAccountStates);
                eventArgs.RemovedTransactions.Add(unmined.Value);
            }

            if (newChainTip != null)
            {
                ChainTip = newChainTip.Identity;
                eventArgs.NewChainTip = newChainTip.Identity;
            }

            OnChangesProcessed(eventArgs);
        }

        public void UpdateAccountProperties(Account account, string name, uint externalKeyCount, uint internalKeyCount, uint importedKeyCount)
        {
            AccountState stats;
            if (!_accounts.TryGetValue(account, out stats))
            {
                stats = new AccountState();
                _accounts[account] = stats;
            }

            stats.AccountName = name;
            stats.ExternalKeyCount = externalKeyCount;
            stats.InternalKeyCount = internalKeyCount;
            stats.ImportedKeyCount = importedKeyCount;

            var eventArgs = new ChangesProcessedEventArgs();
            eventArgs.ModifiedAccountStates[account] = stats;
            OnChangesProcessed(eventArgs);
        }

        private static IEnumerable<WalletTransaction.Output.ControlledOutput> OutputsToAccount(WalletTransaction.Output[] outputs, Account account)
        {
            return outputs.OfType<WalletTransaction.Output.ControlledOutput>().Where(o => o.Account == account);
        }

        public Amount CalculateSpendableBalance(Account account, int minConf)
        {
            var balance = _accounts[account].ZeroConfSpendableBalance;

            if (minConf == 0)
            {
                return balance;
            }

            var unminedTxs = RecentTransactions.UnminedTransactions;
            foreach (var output in unminedTxs.SelectMany(kvp => OutputsToAccount(kvp.Value.Outputs, account)))
            {
                balance -= output.Amount;
            }

            if (minConf == 1)
            {
                return balance;
            }

            var confHeight = BlockChain.ConfirmationHeight(ChainTip.Height, minConf);
            foreach (var block in RecentTransactions.MinedTransactions.ReverseList().TakeWhile(b => b.Height >= confHeight))
            {
                var unconfirmedTxs = block.Transactions;
                foreach (var output in unconfirmedTxs.SelectMany(tx => OutputsToAccount(tx.Outputs, account)))
                {
                    balance -= output.Amount;
                }
            }

            return balance;
        }

        public string OutputDestination(WalletTransaction.Output output)
        {
            if (output is WalletTransaction.Output.ControlledOutput)
            {
                var controlledOutput = (WalletTransaction.Output.ControlledOutput)output;
                if (controlledOutput.Change)
                    return "Change";
                else
                    return _accounts[controlledOutput.Account].AccountName;
            }
            else
            {
                var uncontrolledOutput = (WalletTransaction.Output.UncontrolledOutput)output;
                Address address;
                if (Address.TryFromOutputScript(uncontrolledOutput.PkScript, ActiveChain, out address))
                    return address.Encode();
                else
                    return "Non-address output";
            }
        }

        public AccountState LookupAccountState(Account account) => _accounts[account];
        public string AccountName(Account account) => _accounts[account].AccountName;

        public IEnumerable<KeyValuePair<Account, AccountState>> EnumrateAccounts() => _accounts;

        public static byte[] RandomSeed()
        {
            var seed = new byte[SeedLength];
            using (var prng = new RNGCryptoServiceProvider())
            {
                prng.GetBytes(seed);
            }
            return seed;
        }
    }
}
