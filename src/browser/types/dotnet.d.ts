declare module '*_framework/dotnet.js' {
    export const dotnet: {
        withApplicationArguments(...args: string[]): {
            create(): Promise<{
                setModuleImports(moduleName: string, imports: Record<string, unknown>): void;
                getAssemblyExports(assemblyName: string): Promise<{
                    StopwatchSample: {
                        Reset(): void;
                        Toggle(): boolean;
                    };
                }>;
                getConfig(): { mainAssemblyName: string };
                runMain(): Promise<void>;
            }>;
        };
    };
}
