﻿// Copyright (c) 2016 The btcsuite developers
// Copyright (c) 2016 The Decred developers
// Licensed under the ISC license.  See LICENSE file in the project root for full license information.

using Paymetheus.Decred;
using Paymetheus.Decred.Wallet;
using Paymetheus.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Paymetheus.ViewModels
{
    public sealed class TransactionViewModel : ViewModelBase
    {
        public TransactionViewModel(Wallet wallet, WalletTransaction transaction, BlockIdentity transactionLocation)
        {
            _transaction = transaction;
            _location = transactionLocation;

            var groupedOutputs = _transaction.NonChangeOutputs.Select(o =>
            {
                var destination = wallet.OutputDestination(o);
                return new GroupedOutput(o.Amount, destination);
            }).Aggregate(new List<GroupedOutput>(), (items, next) =>
            {
                var item = items.Find(a => a.Destination == next.Destination);
                if (item == null)
                    items.Add(next);
                else
                    item.Amount += next.Amount;

                return items;
            });

            Confirmations = BlockChain.Confirmations(wallet.ChainTip.Height, transactionLocation);
            Inputs = _transaction.Inputs.Select(input => new Input(-input.Amount, wallet.AccountName(input.PreviousAccount))).ToArray();
            Outputs = _transaction.Outputs.Select(output => new Output(output.Amount, wallet.OutputDestination(output))).ToArray();
            GroupedOutputs = groupedOutputs;
        }

        private readonly WalletTransaction _transaction;

        public struct Input
        {
            public Input(Amount amount, string previousAccount)
            {
                Amount = amount;
                PreviousAccount = previousAccount;
            }

            public Amount Amount { get; }
            public string PreviousAccount { get; }
        }

        public struct Output
        {
            public Output(Amount amount, string destination)
            {
                Amount = amount;
                Destination = destination;
            }

            public Amount Amount { get; }
            public string Destination { get; }
        }

        public Blake256Hash TxHash => _transaction.Hash;
        public Input[] Inputs { get; }
        public Output[] Outputs { get; }
        public Amount? Fee => _transaction.Fee;
        public DateTime LocalSeenTime => _transaction.SeenTime.LocalDateTime;

        private BlockIdentity _location;
        public BlockIdentity Location
        {
            get { return _location; }
            internal set { _location = value; RaisePropertyChanged(); }
        }

        private int _confirmations;
        public int Confirmations
        {
            get { return _confirmations; }
            set { _confirmations = value; RaisePropertyChanged(); }
        }

        public sealed class GroupedOutput
        {
            public Amount Amount { get; set; }
            public string Destination { get; }
            public GroupedOutput(Amount amount, string destination)
            {
                Amount = amount;
                Destination = destination;
            }
        }

        public List<GroupedOutput> GroupedOutputs { get; }
    }
}
