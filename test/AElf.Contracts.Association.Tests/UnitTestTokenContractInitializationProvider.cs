using System;
using System.Collections.Generic;
using AElf.Contracts.MultiToken;
using AElf.Kernel.Consensus.AEDPoS;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Token;
using AElf.OS;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.Options;

namespace AElf.Contracts.Association
{
    public class UnitTestTokenContractInitializationProvider : TokenContractInitializationProvider
    {
        private readonly EconomicOptions _economicOptions;
        private readonly AEDPoSOptions _consensusOptions;

        public UnitTestTokenContractInitializationProvider(
            ITokenContractInitializationDataProvider tokenContractInitializationDataProvider,
            IOptionsSnapshot<EconomicOptions> economicOptions,IOptionsSnapshot<AEDPoSOptions> consensusOptions) : base(
            tokenContractInitializationDataProvider)
        {
            _economicOptions = economicOptions.Value;
            _consensusOptions = consensusOptions.Value;
        }

        public override List<ContractInitializationMethodCall> GetInitializeMethodList(byte[] contractCode)
        {
            var address = Address.FromPublicKey(
                ByteArrayHelper.HexStringToByteArray(_consensusOptions.InitialMinerList[0]));
            var list = new List<ContractInitializationMethodCall>
            {
                new ContractInitializationMethodCall
                {
                    MethodName = nameof(TokenContractContainer.TokenContractStub.Create),
                    Params = new CreateInput
                    {
                        Decimals = _economicOptions.Decimals,
                        Issuer = address,
                        IsBurnable = _economicOptions.IsBurnable,
                        Symbol = _economicOptions.Symbol,
                        TokenName = _economicOptions.TokenName,
                        TotalSupply = _economicOptions.TotalSupply,
                        IsProfitable = true
                    }.ToByteString(),
                },
                new ContractInitializationMethodCall
                {
                    MethodName = nameof(TokenContractContainer.TokenContractStub.Issue),
                    Params = new IssueInput
                    {
                        Symbol = _economicOptions.Symbol,
                        Amount = Convert.ToInt64(_economicOptions.TotalSupply * (1 - _economicOptions.DividendPoolRatio)),
                        To = address,
                        Memo = "Issue native token"
                    }.ToByteString()
                }
            };
            return list;
        }
    }
}