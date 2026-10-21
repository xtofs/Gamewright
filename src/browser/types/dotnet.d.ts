declare module '*_framework/dotnet.js' {
    export const dotnet: {
        create(): Promise<{
            setModuleImports(moduleName: string, imports: Record<string, unknown>): void;
            getAssemblyExports(assemblyName: string): Promise<{
                ChessGame: {
                    Click(x: number, y: number): void;
                    NewGame(): void;
                };
            }>;
            getConfig(): { mainAssemblyName: string };
            runMain(): Promise<void>;
        }>;
    };
}
