﻿using System;

namespace YetAnotherXamlPad
{
    [Serializable]
    public struct EditorStateDto
    {
        public bool UseViewModel { get; set; }

        public string XamlCode { get; set; }

        public string ViewModelCode { get; set; }
    }
}
