namespace Scratch

module SymbolicLink =
    open System
    open System.IO
    open System.Runtime.InteropServices
    open System.Text
    open Microsoft.Win32.SafeHandles

    [<Literal>]
    // TODO - VERIFY THIS OR ELSE BUFFER OVERRUN POTENTIAL
    let maxUnicodePathLength = 520

    /// <remarks>
    /// Refer to http://msdn.microsoft.com/en-us/library/windows/hardware/ff552012%28v=vs.85%29.aspx
    /// </remarks>
    [<CLIMutable>]
    [<StructLayout(LayoutKind.Sequential)>]
    type SymbolicLinkReparseData = {
        ReparseTag          : uint32
        ReparseDataLength   : uint16
        Reserved            : uint16
        SubstituteNameOffset: uint16
        SubstituteNameLength: uint16
        PrintNameOffset     : uint16
        PrintNameLength     : uint16
        Flags               : uint32
        [<MarshalAs(UnmanagedType.ByValArray, SizeConst = maxUnicodePathLength)>]
        PathBuffer          : byte array
    }


    [<Literal>]
    let genericReadAccess = 0x80000000u
    [<Literal>]
    let fileFlagsForOpenReparsePointAndBackupSemantics = 0x02200000u
    [<Literal>]
    let ioctlCommandGetReparsePoint = 0x000900A8u
    [<Literal>]
    let openExisting = 0x3u
    [<Literal>]
    let pathNotAReparsePointError = 0x80071126u
    [<Literal>]
    let shareModeAll = 0x7u     // Read, Write, Delete
    [<Literal>]
    let symLinkTag = 0xA000000Cu
    [<Literal>]
    let targetIsAFile = 0
    [<Literal>]
    let targetIsADirectory = 1
    [<Literal>]
    let xxx = 0x0u

    [<DllImport("kernel32.dll", SetLastError = true)>]
    extern SafeFileHandle CreateFile(
        string lpFileName,
        uint32 dwDesiredAccess,
        uint32 dwShareMode,
        IntPtr lpSecurityAttributes,
        uint32 dwCreationDisposition,
        uint32 dwFlagsAndAttributes,
        IntPtr hTemplateFile)

    [<DllImport("kernel32.dll", SetLastError = true)>]
    extern bool CreateSymbolicLink(
        string lpSymlinkFileName,
        string lpTargetFileName,
        int dwFlags)


    [<DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)>]
    extern bool DeviceIoControl(
        IntPtr hDevice,
        uint32 dwIoControlCode,
        IntPtr lpInBuffer,
        int nInBufferSize,
        IntPtr lpOutBuffer,
        int nOutBufferSize,
        int& lpBytesReturned,
        IntPtr lpOverlapped)

    let getFileHandle (path: string) =
        CreateFile(path, genericReadAccess, shareModeAll, IntPtr.Zero, openExisting, fileFlagsForOpenReparsePointAndBackupSemantics, IntPtr.Zero)

    let public createDirectoryLink (linkPath: string, targetPath: string) =
        if (CreateSymbolicLink(linkPath, targetPath, targetIsADirectory) = false || Marshal.GetLastWin32Error() <> 0) then
            try
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error())
            with
                :? COMException as ex -> raise <| new IOException(ex.Message, ex)

    let public createFileLink (linkPath: string, targetPath: string) =
        if (CreateSymbolicLink(linkPath, targetPath, targetIsAFile) = false) then
            Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error())

(*
        public static bool Exists(string path)
        {
            if (!Directory.Exists(path) && !File.Exists(path))
            {
                return false;
            }
            string target = GetTarget(path);
            return target != null;
        }
*)


    let public getTarget (path: string) : string =
         use fileHandle = getFileHandle path
         if fileHandle.IsInvalid then
            Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error())
            null
         else
            let outBufferSize = Marshal.SizeOf(typeof<SymbolicLinkReparseData>)
            let mutable outBuffer = IntPtr.Zero
            try
                outBuffer <- Marshal.AllocHGlobal(outBufferSize)
                let mutable bytesReturned = 0
                let success = DeviceIoControl(fileHandle.DangerousGetHandle(), ioctlCommandGetReparsePoint, IntPtr.Zero, 0, outBuffer, outBufferSize, &bytesReturned, IntPtr.Zero)
                fileHandle.Close()
            
                if success then
                    let reparseDataBuffer = Marshal.PtrToStructure(outBuffer, typeof<SymbolicLinkReparseData>) :?> SymbolicLinkReparseData
                    if reparseDataBuffer.ReparseTag <> symLinkTag then
                        null
                    else
                        System.Text.Encoding.Unicode.GetString(reparseDataBuffer.PathBuffer, int reparseDataBuffer.PrintNameOffset, int reparseDataBuffer.PrintNameLength)
                else
                    let error = uint32 (Marshal.GetHRForLastWin32Error())// :?> uint
                    if error = pathNotAReparsePointError then
                        null
                    else
                        Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error())
                        null
            finally
                Marshal.FreeHGlobal(outBuffer)
           

(* 
        public static string GetTarget(string path)
        {
            SymbolicLinkReparseData reparseDataBuffer;
 
            using (SafeFileHandle fileHandle = getFileHandle(path))
            {
                if (fileHandle.IsInvalid)
                {
                    Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                }
 
                int outBufferSize = Marshal.SizeOf(typeof(SymbolicLinkReparseData));
                IntPtr outBuffer = IntPtr.Zero;
                try
                {
                    outBuffer = Marshal.AllocHGlobal(outBufferSize);
                    int bytesReturned;
                    bool success = DeviceIoControl(
                        fileHandle.DangerousGetHandle(), ioctlCommandGetReparsePoint, IntPtr.Zero, 0,
                        outBuffer, outBufferSize, out bytesReturned, IntPtr.Zero);
 
                    fileHandle.Close();
 
                    if (!success)
                    {
                        if (((uint)Marshal.GetHRForLastWin32Error()) == pathNotAReparsePointError)
                        {
                            return null;
                        }
                        Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                    }
 
                    reparseDataBuffer = (SymbolicLinkReparseData)Marshal.PtrToStructure(
                        outBuffer, typeof(SymbolicLinkReparseData));
                }
                finally
                {
                    Marshal.FreeHGlobal(outBuffer);
                }
            }
            if (reparseDataBuffer.ReparseTag != symLinkTag)
            {
                return null;
            }
 
            string target = Encoding.Unicode.GetString(reparseDataBuffer.PathBuffer,
                reparseDataBuffer.PrintNameOffset, reparseDataBuffer.PrintNameLength);
 
            return target;
        }
*)

    let public exists (path: string) : bool =
        if (not (Directory.Exists(path)) && not (File.Exists(path))) then
            false
        else
            let target = getTarget path
            target <> null
