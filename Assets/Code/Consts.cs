using BeauUtil;

namespace Journalism {
    static public class Events {
        static public readonly StringHash32 VariableUpdated = "save:variable-updated"; // TableKeyPair variableId
        static public readonly StringHash32 SaveDeclared = "save:declared"; // PlayerData data
    }
}