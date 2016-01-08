﻿// Copyright (c) 2016 The btcsuite developers
// Licensed under the ISC license.  See LICENSE file in the project root for full license information.

using System;

namespace Paymetheus.Bitcoin.Wallet
{
    public struct Account : IEquatable<Account>
    {
        public Account(uint accountNumber)
        {
            AccountNumber = accountNumber;
        }

        public uint AccountNumber { get; }

        public static bool operator ==(Account lhs, Account rhs) => lhs.AccountNumber == rhs.AccountNumber;

        public static bool operator !=(Account lhs, Account rhs) => !(lhs == rhs);

        public bool Equals(Account other) => this == other;

        public override bool Equals(object obj) => (obj is Account) && (Account)obj == this;

        public override int GetHashCode() => AccountNumber.GetHashCode();
    }

    public sealed class AccountState
    {
        public string AccountName { get; set; }
        public Amount TotalBalance { get; set; }
        public Amount ImmatureCoinbaseReward { get; set; }
        public Amount ZeroConfSpendableBalance => TotalBalance - ImmatureCoinbaseReward;

        public uint ExternalKeyCount { get; set; }
        public uint InternalKeyCount { get; set; }
        public uint ImportedKeyCount { get; set; }
    }
}
