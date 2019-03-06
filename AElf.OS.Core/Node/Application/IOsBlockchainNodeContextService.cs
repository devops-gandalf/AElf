using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.KernelAccount;
using AElf.Kernel.Node.Application;
using AElf.Kernel.Node.Domain;
using AElf.Kernel.SmartContract.Application;
using AElf.OS.Network.Infrastructure;
using AElf.OS.Node.Domain;
using AElf.Types.CSharp;
using Google.Protobuf;

namespace AElf.OS.Node.Application
{
    public class GenesisSmartContractDto
    {
        public Type SmartContractType { get; set; }
        public string SystemSmartContractName { get; set; }
    }

    public class OsBlockchainNodeContextStartDto
    {
        public int ChainId { get; set; }

        public List<GenesisSmartContractDto> InitializationSmartContracts { get; set; } =
            new List<GenesisSmartContractDto>();

        public Transaction[] InitializationTransactions { get; set; }
    }

    public static class GenesisSmartContractDtoExtensions
    {
        public static void AddGenesisSmartContract(this List<GenesisSmartContractDto> genesisSmartContracts,
            Type smartContractType)
        {
            genesisSmartContracts.Add(new GenesisSmartContractDto()
            {
                SmartContractType = smartContractType,
                SystemSmartContractName = smartContractType.FullName
            });
        }

        public static void AddGenesisSmartContract<T>(this List<GenesisSmartContractDto> genesisSmartContracts)
        {
            genesisSmartContracts.Add(new GenesisSmartContractDto()
            {
                SmartContractType = typeof(T),
                SystemSmartContractName = typeof(T).FullName
            });
        }

        public static void AddGenesisSmartContracts(this List<GenesisSmartContractDto> genesisSmartContracts,
            params Type[] smartContractTypes)
        {
            foreach (var smartContractType in smartContractTypes)
            {
                AddGenesisSmartContract(genesisSmartContracts, smartContractType);
            }
        }
    }

    public interface IOsBlockchainNodeContextService
    {
        Task<OsBlockchainNodeContext> StartAsync(OsBlockchainNodeContextStartDto dto);

        Task StopAsync(OsBlockchainNodeContext blockchainNodeContext);
    }

    public class OsBlockchainNodeContextService : IOsBlockchainNodeContextService
    {
        private IBlockchainNodeContextService _blockchainNodeContextService;
        private IAElfNetworkServer _networkServer;

        public OsBlockchainNodeContextService(IBlockchainNodeContextService blockchainNodeContextService,
            IAElfNetworkServer networkServer)
        {
            _blockchainNodeContextService = blockchainNodeContextService;
            _networkServer = networkServer;
        }

        public async Task<OsBlockchainNodeContext> StartAsync(OsBlockchainNodeContextStartDto dto)
        {
            BlockchainNodeContextStartDto blockchainNodeContextStartDto = new BlockchainNodeContextStartDto()
            {
                ChainId = dto.ChainId,
                Transactions = dto.InitializationSmartContracts
                    .Select(p =>
                        GetTransactionForDeployment(dto.ChainId, p.SmartContractType, p.SystemSmartContractName))
                    .ToArray()
            };

            var context = new OsBlockchainNodeContext
            {
                BlockchainNodeContext =
                    await _blockchainNodeContextService.StartAsync(blockchainNodeContextStartDto)
            };
            context.AElfNetworkServer = _networkServer;

            await _networkServer.StartAsync();

            return context;
        }


        private Transaction GetTransactionForDeployment(int chainId, Type contractType, string systemContractName)
        {
            var zeroAddress = Address.BuildContractAddress(chainId, 0);
            var code = File.ReadAllBytes(contractType.Assembly.Location);

            return new Transaction()
            {
                From = zeroAddress,
                To = zeroAddress,
                MethodName = nameof(ISmartContractZero.DeploySystemSmartContract),
                // TODO: change cagtegory to 0
                Params = ByteString.CopyFrom(ParamsPacker.Pack(Hash.FromString(systemContractName), 2, code))
            };
        }

        public async Task StopAsync(OsBlockchainNodeContext blockchainNodeContext)
        {
            await _networkServer.StopAsync();

            await _blockchainNodeContextService.StopAsync(blockchainNodeContext.BlockchainNodeContext);
        }
    }
}