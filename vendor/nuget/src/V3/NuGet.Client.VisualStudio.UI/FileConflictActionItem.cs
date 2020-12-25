﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Client.VisualStudio.UI
{
    // Represents an item in the FileConflictAction combobox.
    public class FileConflictActionItem
    {
        public string Text
        {
            get;
            private set;
        }

        public FileConflictAction Action
        {
            get;
            private set;
        }

        public FileConflictActionItem(string text, FileConflictAction action)
        {
            Text = text;
            Action = action;
        }

        public override string ToString()
        {
            return Text;
        }
    }
}
