﻿// Copyright (c) 2016 The btcsuite developers
// Licensed under the ISC license.  See LICENSE file in the project root for full license information.

using Paymetheus.Bitcoin.Util;
using System;
using System.Collections.Generic;

namespace Paymetheus.Bitcoin.Wallet
{
    public sealed class Block
    {
        public Block(Sha256Hash hash, int height, long unixTime, List<WalletTransaction> transactions)
        {
            if (hash == null)
                throw new ArgumentNullException(nameof(hash));
            if (transactions == null)
                throw new ArgumentNullException(nameof(transactions));

            Identity = new BlockIdentity(hash, height);
            Timestamp = DateTimeOffsetExtras.FromUnixTimeSeconds(unixTime);
            Transactions = transactions;
        }

        public BlockIdentity Identity { get; }
        public int Height => Identity.Height;
        public Sha256Hash Hash => Identity.Hash;
        public DateTimeOffset Timestamp { get; }
        public List<WalletTransaction> Transactions { get; }
    }
}
