import assert from 'node:assert/strict';
import test from 'node:test';
import { resolveRuntimeTarget } from '../scripts/runtime-targets.mjs';

for (const [platform, os, executable, runtime] of [
  ['win32', 'win', 'ServiceBusViewer.exe', 'coreclr.dll'],
  ['linux', 'linux', 'ServiceBusViewer', 'libcoreclr.so'],
  ['darwin', 'osx', 'ServiceBusViewer', 'libcoreclr.dylib'],
]) {
  for (const arch of ['x64', 'arm64']) {
    test(`${platform}/${arch} selects matching native assets`, () => {
      assert.deepEqual(resolveRuntimeTarget(platform, arch), { rid: `${os}-${arch}`, executable, coreRuntime: runtime });
    });
  }
}
for (const [platform, arch] of [['freebsd', 'x64'], ['win32', 'ia32'], ['linux', 'arm'], ['toString', 'x64']]) {
  test(`rejects ${platform}/${arch}`, () => assert.throws(() => resolveRuntimeTarget(platform, arch), /Unsupported desktop target/));
}
