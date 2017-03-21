﻿// Copyright (c) 2016 The btcsuite developers
// Copyright (c) 2016 The Decred developers
// Licensed under the ISC license.  See LICENSE file in the project root for full license information.

using Paymetheus.Decred.Script;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Paymetheus.Decred.Wallet
{
    public static class TransactionAuthor
    {
        /// <summary>
        /// Provides transaction inputs referencing spendable outputs to construct a
        /// transaction with some target output amount.
        /// </summary>
        /// <param name="target">Target amount the previous output value must meet or exceed.</param>
        /// <returns>Total previous output amount and array of transaction inputs if the target was met.</returns>
        public delegate Task<Tuple<Amount, Transaction.Input[]>> InputSource(Amount target);

        /// <summary>
        /// Constructs an unsigned transaction by referencing previous unspent outputs.
        /// A change output is added when necessary to return extra value back to the wallet.
        /// </summary>
        /// <param name="outputs">Transaction output array without change.</param>
        /// <param name="changeScript">Output script to pay change to.</param>
        /// <param name="fetchInputsAsync">Input selection source.</param>
        /// <returns>Unsigned transaction and total input amount.</returns>
        /// <exception cref="InsufficientFundsException">Input source was unable to provide enough input value.</exception>
        public static async Task<Tuple<Transaction, Amount>> BuildUnsignedTransaction(Transaction.Output[] outputs,
                                                                                      OutputScript changeScript,
                                                                                      Amount feePerKb,
                                                                                      InputSource fetchInputsAsync)
        {
            if (outputs == null)
                throw new ArgumentNullException(nameof(outputs));
            if (changeScript == null)
                throw new ArgumentNullException(nameof(changeScript));
            if (fetchInputsAsync == null)
                throw new ArgumentNullException(nameof(fetchInputsAsync));

            var targetAmount = outputs.Sum(o => o.Amount);
            var estimatedSize = Transaction.EstimateSerializeSize(1, outputs, true);
            var targetFee = TransactionFees.FeeForSerializeSize(feePerKb, estimatedSize);

            while (true)
            {
                var funding = await fetchInputsAsync(targetAmount + targetFee);
                var inputAmount = funding.Item1;
                var inputs = funding.Item2;
                if (inputAmount < targetAmount + targetFee)
                {
                    throw new InsufficientFundsException();
                }

                var unsignedTransaction = new Transaction(Transaction.SupportedVersion, inputs, outputs, 0, 0);
                if (inputAmount > targetAmount + targetFee)
                {
                    unsignedTransaction = TransactionFees.AddChange(unsignedTransaction, inputAmount,
                        changeScript, feePerKb);
                }

                if (TransactionFees.EstimatedFeePerKb(unsignedTransaction, inputAmount) < feePerKb)
                {
                    estimatedSize = Transaction.EstimateSerializeSize(inputs.Length, outputs, true);
                    targetFee = TransactionFees.FeeForSerializeSize(feePerKb, estimatedSize);
                }
                else
                {
                    return Tuple.Create(unsignedTransaction, inputAmount);
                }
            }
        }
    }

    public class InsufficientFundsException : Exception
    {
        public InsufficientFundsException() : base("Insufficient funds available to construct transaction.") { }
    }
}

