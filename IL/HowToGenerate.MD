# Updating `liblinux.il`

When there are updates to the liblinux.dll in VS. We will need to update the il for SSHDebugPS.

You can get the dll at `<Installed_VS_Root>\Common7\IDE\PrivateAssemblies\liblinux.dll`.

## How to generate `liblinux.il` from `liblinux.dll`

1. Go to (VsSrcRoot)\src\tools\X86\Managed\V4.0 (or run `dir /S asmmeta.exe` in `(VsSrcRoot)\src\tools\X86\` and use the directory that contains `asmmeta.exe`).
2. Run `asmmeta "(folder_to_liblinux)\liblinux.dll" /config=(VsSrcRoot)\src\tools\preserveSecurityAttributes.asmmeta.config.xml /config=(VsSrcRoot)\src\tools\asmmeta.config.xml /config=(VsSrcRoot)\src\debugger\Debugger.CopyReferenceIL.il.config.xml /out:(output)\liblinux.il`

Variables:
- `(VsSrcRoot)` = root of the VS enlistment
- `(folder_to_liblinux)` = where the liblinux.dll file is located to generate il from
- `(output)` = where the liblinux.il file should be saved.