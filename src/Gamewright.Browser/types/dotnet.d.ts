// The module the app's _framework/dotnet.js exports; see the dynamic import in main.ts.
interface DotnetModule {
    dotnet: {
        create(): Promise<{
            setModuleImports(moduleName: string, imports: Record<string, unknown>): void;
            runMain(): Promise<void>;
        }>;
    };
}
