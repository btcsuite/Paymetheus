// Copyright (c) 2016 The btcsuite developers
// Licensed under the ISC license.  See LICENSE file in the project root for full license information.

using Paymetheus.Bitcoin;
using Paymetheus.Bitcoin.Wallet;
using System;

namespace Paymetheus
{
    sealed class RecentAccountViewModel : ViewModelBase
    {
        public RecentAccountViewModel(ViewModelBase parent, Account account, AccountProperties properties)
            : base(parent)
        {
            if (parent == null)
                throw new ArgumentNullException(nameof(parent));
            if (properties == null)
                throw new ArgumentNullException(nameof(properties));

            Account = account;
            _accountProperties = properties;
        }

        private readonly AccountProperties _accountProperties;

        public Account Account { get; }
        public string AccountName => _accountProperties.AccountName;
        public string BalanceString => Denomination.Bitcoin.FormatAmount(_accountProperties.TotalBalance);

        public void NotifyPropertiesChanged()
        {
            RaisePropertyChanged(nameof(AccountName));
            RaisePropertyChanged(nameof(BalanceString));
        }
    }
}
