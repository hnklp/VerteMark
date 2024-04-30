using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VerteMark.ObjectClasses
{
    public enum AppState
    {
        Drawing,
        Erasing,
        Cropping
    }

    public class StateManager
    {
        public event EventHandler<AppState> StateChanged;

        private AppState currentState;

        public AppState CurrentState
        {
            get { return currentState; }
            set
            {
                currentState = value;
                StateChanged?.Invoke(this, currentState);
            }
        }
    }
}
