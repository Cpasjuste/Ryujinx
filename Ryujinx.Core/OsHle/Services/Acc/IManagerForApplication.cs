using Ryujinx.Core.OsHle.Ipc;
using System.Collections.Generic;

namespace Ryujinx.Core.OsHle.IpcServices.Acc
{
    class IManagerForApplication : IIpcService
    {
        private Dictionary<int, ServiceProcessRequest> m_Commands;

        public IReadOnlyDictionary<int, ServiceProcessRequest> Commands => m_Commands;

        public IManagerForApplication()
        {
            m_Commands = new Dictionary<int, ServiceProcessRequest>()
            {
                { 0, CheckAvailability },
                { 1, GetAccountId      }
            };
        }

        public long CheckAvailability(ServiceCtx Context)
        {           
            return 0;
        }

        public long GetAccountId(ServiceCtx Context)
        {
            Context.ResponseData.Write(0xcafeL);

            return 0;
        }
    }
}