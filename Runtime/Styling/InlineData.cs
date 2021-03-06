using ReactUnity.Helpers;

namespace ReactUnity.Styling
{
    public class InlineData : WatchableObjectRecord
    {
        internal readonly string Identifier;

        public InlineData(string identifier = null) : base()
        {
            Identifier = identifier;
        }
    }
}
