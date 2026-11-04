# 解析專案的標準打包路徑；只檢查檔案身分，不搬動或複製程式碼。
param(
    [Parameter(Mandatory = $true)][string]$ProjectFile,
    [string]$SourcesDirectory
)

$ErrorActionPreference = 'Stop'

# 開啟專案檔後取得實體路徑，藉此辨認目錄連結。
# 路徑文字或內容雜湊相同，都不足以證明是同一份實體檔案。
Add-Type -TypeDefinition @'
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;
public static class OccidentPhysicalPath {
    // 由已開啟的檔案控制代碼取得連結解析後路徑，無須改變目錄結構。
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern uint GetFinalPathNameByHandle(
        SafeFileHandle handle, StringBuilder path, uint size, uint flags);
    public static string Get(string path) {
        using (var file = File.Open(path, FileMode.Open, FileAccess.Read,
                                    FileShare.ReadWrite | FileShare.Delete)) {
            var buffer = new StringBuilder(32768);
            uint length = GetFinalPathNameByHandle(file.SafeFileHandle, buffer,
                                                  (uint)buffer.Capacity, 0);
            if (length == 0) throw new Win32Exception(Marshal.GetLastWin32Error());
            if (length >= buffer.Capacity) throw new IOException("Resolved path is too long.");
            return buffer.ToString();
        }
    }
}
'@

$projectPath = [IO.Path]::GetFullPath($ProjectFile)
$projectDirectory = [IO.Path]::GetDirectoryName($projectPath)
if ([string]::IsNullOrWhiteSpace($SourcesDirectory)) {
    $SourcesDirectory = Join-Path ([Environment]::GetFolderPath('MyDocuments')) 'My Games\Terraria\tModLoader\ModSources'
}
$sourcesPath = [IO.Path]::GetFullPath($SourcesDirectory)
$candidate = Join-Path (Join-Path $sourcesPath ([IO.Path]::GetFileName($projectDirectory))) ([IO.Path]::GetFileName($projectPath))

if (Test-Path -LiteralPath $candidate -PathType Leaf) {
    if (-not [string]::Equals([OccidentPhysicalPath]::Get($projectPath),
                             [OccidentPhysicalPath]::Get($candidate),
                             [StringComparison]::OrdinalIgnoreCase)) {
        throw 'ModSources contains another checkout with the same name. Build that checkout or set OccidentModSourcesDirectory to the intended tModLoader source root.'
    }
    $projectDirectory = [IO.Path]::GetDirectoryName($candidate)
}

# 輸出格式需與 tModLoader 掃描目錄所得的絕對 Windows 路徑一致。
[IO.Path]::GetFullPath($projectDirectory)
