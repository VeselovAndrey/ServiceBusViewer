import { mkdirSync, readFileSync, renameSync, rmSync, writeFileSync } from 'node:fs';
import path from 'node:path';

export const defaultWindowSize = Object.freeze({ width: 1280, height: 1600 });

function validSize(value) {
  return value !== null && typeof value === 'object'
    && Number.isSafeInteger(value.width) && value.width > 0
    && Number.isSafeInteger(value.height) && value.height > 0;
}

export function clampWindowSize(size, workArea) {
  const preferred = validSize(size) ? size : defaultWindowSize;
  return { width: Math.min(preferred.width, workArea.width), height: Math.min(preferred.height, workArea.height) };
}

export function createWindowSizeStore(directory) {
  const file = path.join(directory, 'window-size.json');
  const temporary = `${file}.tmp`;
  let pending;
  let timer;
  return {
    load() {
      try {
        const value = JSON.parse(readFileSync(file, 'utf8'));
        if (validSize(value)) return { width: value.width, height: value.height };
      } catch { /* Missing or damaged preferences use the default size. */ }
      return { ...defaultWindowSize };
    },
    remember(size) {
      if (!validSize(size)) return;
      pending = { width: size.width, height: size.height };
      clearTimeout(timer);
      timer = setTimeout(() => this.flush(), 300);
      timer.unref?.();
    },
    flush() {
      clearTimeout(timer);
      if (!pending) return;
      try {
        mkdirSync(directory, { recursive: true });
        writeFileSync(temporary, JSON.stringify(pending), { encoding: 'utf8', mode: 0o600 });
        renameSync(temporary, file);
        pending = undefined;
      } catch {
        // Preferences are best effort and must never prevent application shutdown.
        try { rmSync(temporary, { force: true }); } catch { /* Leave existing preferences intact. */ }
      }
    },
  };
}
