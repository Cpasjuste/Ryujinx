namespace Ryujinx.Core.OsHle.IpcServices.FspSrv
{
    static class FsErr
    {
        public const int PathDoesNotExist  = 1;
        public const int PathAlreadyExists = 2;
        public const int PathAlreadyInUse  = 7;
    }
}