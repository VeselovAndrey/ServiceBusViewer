import { copyFile, mkdir } from "node:fs/promises";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const rootDirectory = resolve(dirname(fileURLToPath(import.meta.url)), "..");

const filesToCopy = [
	[
		"node_modules/@fontsource/inter/files/inter-latin-400-normal.woff2",
		"src/ServiceBusViewer/wwwroot/fonts/inter/inter-latin-400-normal.woff2"
	],
	[
		"node_modules/@fontsource/inter/files/inter-latin-500-normal.woff2",
		"src/ServiceBusViewer/wwwroot/fonts/inter/inter-latin-500-normal.woff2"
	],
	[
		"node_modules/@fontsource/inter/files/inter-latin-600-normal.woff2",
		"src/ServiceBusViewer/wwwroot/fonts/inter/inter-latin-600-normal.woff2"
	],
	[
		"node_modules/@fontsource/inter/files/inter-latin-700-normal.woff2",
		"src/ServiceBusViewer/wwwroot/fonts/inter/inter-latin-700-normal.woff2"
	],
	[
		"node_modules/@fontsource/jetbrains-mono/files/jetbrains-mono-latin-400-normal.woff2",
		"src/ServiceBusViewer/wwwroot/fonts/jetbrains-mono/jetbrains-mono-latin-400-normal.woff2"
	],
	[
		"node_modules/@fontsource/jetbrains-mono/files/jetbrains-mono-latin-500-normal.woff2",
		"src/ServiceBusViewer/wwwroot/fonts/jetbrains-mono/jetbrains-mono-latin-500-normal.woff2"
	],
	[
		"node_modules/material-design-icons-iconfont/dist/fonts/MaterialIcons-Regular.woff2",
		"src/ServiceBusViewer/wwwroot/fonts/material-icons/MaterialIcons-Regular.woff2"
	],
	[
		"node_modules/material-design-icons-iconfont/dist/fonts/MaterialIcons-Regular.woff",
		"src/ServiceBusViewer/wwwroot/fonts/material-icons/MaterialIcons-Regular.woff"
	]
];

for (const [sourceRelativePath, destinationRelativePath] of filesToCopy) {
	const sourcePath = resolve(rootDirectory, sourceRelativePath);
	const destinationPath = resolve(rootDirectory, destinationRelativePath);

	await mkdir(dirname(destinationPath), { recursive: true });
	await copyFile(sourcePath, destinationPath);
}
