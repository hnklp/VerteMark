using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VerteMark.ObjectClasses {
    internal class User {

        public string Id { get; private set; }
        public bool IsValidator { get; private set; }

        public User(string Id, bool Validator) {
            this.Id = Id;
            IsValidator = Validator;
        }


    }
}
