declare module '*_framework/dotnet.js' {
    export const dotnet: {
        create(): Promise<{
            setModuleImports(moduleName: string, imports: Record<string, unknown>): void;
            runMain(): Promise<void>;
        }>;
    };
}
