export function resolveRuntimeTarget(platform = process.platform, arch = process.arch) {
  const operatingSystems = {
    win32: 'win',
    linux: 'linux',
    darwin: 'osx',
  };
  if (!Object.hasOwn(operatingSystems, platform) || !['x64', 'arm64'].includes(arch)) {
    throw new Error(`Unsupported desktop target: ${platform}/${arch}. Use win32, linux or darwin with x64 or arm64.`);
  }
  // Map the requested native target to the names used by .NET and Electron packaging.
  return {
    rid: `${operatingSystems[platform]}-${arch}`,
    executable: platform === 'win32' ? 'ServiceBusViewer.exe' : 'ServiceBusViewer',
    coreRuntime: platform === 'win32' ? 'coreclr.dll' : platform === 'darwin' ? 'libcoreclr.dylib' : 'libcoreclr.so',
  };
}
