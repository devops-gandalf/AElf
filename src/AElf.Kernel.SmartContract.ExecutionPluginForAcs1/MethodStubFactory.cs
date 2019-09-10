using System;
using System.Threading.Tasks;
using AElf.CSharp.Core;
using Google.Protobuf;

namespace AElf.Kernel.SmartContract.ExecutionPluginForAcs1
{
    public class MethodStubFactory : IMethodStubFactory
    {
        private readonly IHostSmartContractBridgeContext _context;

        public MethodStubFactory(IHostSmartContractBridgeContext context)
        {
            _context = context;
        }

        #pragma warning disable 1998
        public IMethodStub<TInput, TOutput> Create<TInput, TOutput>(Method<TInput, TOutput> method)
            where TInput : IMessage<TInput>, new() where TOutput : IMessage<TOutput>, new()
        {
            async Task<IExecutionResult<TOutput>> SendAsync(TInput input)
            {
                var tcs = new TaskCompletionSource<IExecutionResult<TOutput>>();
                tcs.SetException(new NotSupportedException());
                return await tcs.Task;
            }

            var context = _context;

            async Task<TOutput> CallAsync(TInput input)
            {
                return _context.Call<TOutput>(context.Self, method.Name,
                    input.ToByteString());
            }

            return new MethodStub<TInput, TOutput>(method, SendAsync, CallAsync);
        }
    }
}