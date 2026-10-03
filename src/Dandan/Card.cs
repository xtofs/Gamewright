namespace Dandan;

public enum Card
{
    Dandan,
    AccumulatedKnowledge,
    Brainstorm,
    CrystalSpray,
    DanceOfTheSkywise,
    MemoryLapse,
    Metamorphose,
    MindBend,
    MysticalTutor,
    Predict,
    RayOfCommand,
    SupplantForm,
    Unsubstantiate,
    VisionCharm,
    DiminishingReturns,
    MysticRetrieval,
    HalimarDepths,
    Island,
    IzzetBoilerworks,
    LonelySandbar,
    MysticSanctuary,
    RemoteIsle,
    SvyeluniteTemple,
    TempleOfEpiphany,

}

// Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
public record FrameSize(int w, int h);

public record Root(string image, Size size, FrameSize frameSize, int columns, int rows, Dictionary<string, Frame> frames);

public record Size(int w, int h);
public record Frame(int x, int y, int w, int h);




