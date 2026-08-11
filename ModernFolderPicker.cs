using System.Runtime.InteropServices;

namespace PanelExtractor
{
    // .NET Framework's FolderBrowserDialog always uses the legacy tree view.
    // This calls Windows' modern Common Item Dialog directly without adding a package.
    internal static class ModernFolderPicker
    {
        private const int CancelledHResult = unchecked((int)0x800704C7);

        public static string? Show(
            IWin32Window owner,
            string title,
            string initialFolder)
        {
            IFileDialog? dialog = null;
            IShellItem? initialFolderItem = null;
            IShellItem? selectedItem = null;

            try
            {
                dialog = (IFileDialog)new FileOpenDialog();
                dialog.GetOptions(out FileDialogOptions options);
                dialog.SetOptions(
                    options |
                    FileDialogOptions.PickFolders |
                    FileDialogOptions.ForceFileSystem |
                    FileDialogOptions.PathMustExist);
                dialog.SetTitle(title);

                if (Directory.Exists(initialFolder))
                {
                    initialFolderItem = CreateShellItem(initialFolder);
                    dialog.SetFolder(initialFolderItem);
                }

                int result = dialog.Show(owner.Handle);
                if (result == CancelledHResult)
                {
                    return null;
                }

                Marshal.ThrowExceptionForHR(result);
                dialog.GetResult(out selectedItem);
                selectedItem.GetDisplayName(DisplayNameType.FileSystemPath, out IntPtr pathPointer);

                try
                {
                    return Marshal.PtrToStringUni(pathPointer);
                }
                finally
                {
                    Marshal.FreeCoTaskMem(pathPointer);
                }
            }
            finally
            {
                ReleaseComObject(selectedItem);
                ReleaseComObject(initialFolderItem);
                ReleaseComObject(dialog);
            }
        }

        private static IShellItem CreateShellItem(string path)
        {
            Guid interfaceId = typeof(IShellItem).GUID;
            SHCreateItemFromParsingName(path, IntPtr.Zero, ref interfaceId, out IShellItem item);
            return item;
        }

        private static void ReleaseComObject(object? value)
        {
            if (value is not null && Marshal.IsComObject(value))
            {
                Marshal.FinalReleaseComObject(value);
            }
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
        private static extern void SHCreateItemFromParsingName(
            string path,
            IntPtr bindingContext,
            ref Guid interfaceId,
            [MarshalAs(UnmanagedType.Interface)] out IShellItem shellItem);

        [Flags]
        private enum FileDialogOptions : uint
        {
            PickFolders = 0x00000020,
            ForceFileSystem = 0x00000040,
            PathMustExist = 0x00000800
        }

        private enum DisplayNameType : uint
        {
            FileSystemPath = 0x80058000
        }

        [ComImport]
        [Guid("DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7")]
        private class FileOpenDialog
        {
        }

        [ComImport]
        [Guid("42F85136-DB7E-439C-85F1-E4075D135FC8")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IFileDialog
        {
            [PreserveSig]
            int Show(IntPtr parent);

            void SetFileTypes(uint fileTypeCount, IntPtr filterSpecifications);
            void SetFileTypeIndex(uint fileTypeIndex);
            void GetFileTypeIndex(out uint fileTypeIndex);
            void Advise(IntPtr events, out uint cookie);
            void Unadvise(uint cookie);
            void SetOptions(FileDialogOptions options);
            void GetOptions(out FileDialogOptions options);
            void SetDefaultFolder(IShellItem shellItem);
            void SetFolder(IShellItem shellItem);
            void GetFolder(out IShellItem shellItem);
            void GetCurrentSelection(out IShellItem shellItem);
            void SetFileName([MarshalAs(UnmanagedType.LPWStr)] string name);
            void GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string name);
            void SetTitle([MarshalAs(UnmanagedType.LPWStr)] string title);
            void SetOkButtonLabel([MarshalAs(UnmanagedType.LPWStr)] string text);
            void SetFileNameLabel([MarshalAs(UnmanagedType.LPWStr)] string label);
            void GetResult(out IShellItem shellItem);
        }

        [ComImport]
        [Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellItem
        {
            void BindToHandler(
                IntPtr bindingContext,
                ref Guid handlerId,
                ref Guid interfaceId,
                out IntPtr result);

            void GetParent(out IShellItem parent);
            void GetDisplayName(DisplayNameType displayNameType, out IntPtr name);
            void GetAttributes(uint attributeMask, out uint attributes);
            void Compare(IShellItem shellItem, uint hint, out int order);
        }
    }
}
