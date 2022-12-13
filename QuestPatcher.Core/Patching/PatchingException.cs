using System;

namespace QuestPatcher.Core.Patching
{
    public class PatchingException : Exception
    {
        public PatchingException(string message) : base(message) { }
        public PatchingException(string? message, Exception cause) : base(message, cause) { }
    }

    public class GameNotExistException : PatchingException
    {
        public GameNotExistException(string message) : base(message) { }
    }
    
    public class GameIsCrackedException : PatchingException
    {
        public GameIsCrackedException(string message) : base(message) { }
    }
    
    public class GameVersionParsingException : PatchingException
    {
        public GameVersionParsingException(string message) : base(message) { }
        
        public GameVersionParsingException(string? message, Exception cause) : base(message, cause) { }
    }

    public class GameTooOldException : PatchingException
    {
        public GameTooOldException(string message): base(message) { }
    }
}
