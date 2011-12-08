using System;

namespace Microsoft.Xna.Framework
{
    public class GameComponentCollectionEventArgs : EventArgs
    {
        private IGameComponent _gameComponent;

        public GameComponentCollectionEventArgs(IGameComponent gameComponent)
        {
            _gameComponent = gameComponent;
        }

        public IGameComponent GameComponent
        {
            get
            {
                return _gameComponent;
            }
        }
    }
}

