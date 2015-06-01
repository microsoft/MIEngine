// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using MICore;

namespace Microsoft.MIDebugEngine
{
    public class DebuggedModule
    {
        private uint _loadOrder;
        private const ulong INVALID_ADDRESS = 0xffffffffffffffff;
        public DebuggedModule(string id, string name, ulong baseAddr, ulong size, bool symbolsLoaded, string symPath, uint loadOrder)
        {
            Id = id;
            Name = name;
            Sections = new List<Section>();
            Sections.Add(new Section(String.Empty, baseAddr, size));               // TODO real module info
            SymbolsLoaded = symbolsLoaded;
            SymbolPath = symPath;                    // symbols in module
            _loadOrder = loadOrder;
        }

        public DebuggedModule(string id, string name, ValueListValue sections, bool symbolsLoaded, uint loadOrder)
        {
            Id = id;
            Name = name;
            Sections = new List<Section>();
            LoadSections(sections);
            SymbolsLoaded = symbolsLoaded;
            SymbolPath = name;                    // symbols in module
            _loadOrder = loadOrder;
        }


        private void LoadSections(ValueListValue sectionList)
        {
            if (sectionList == null)
            {
                return;
            }
            List<Section> sections = new List<Section>();
            foreach (var s in sectionList.Content)
            {
                string name = s.FindString("name");
                ulong addr = s.FindAddr("addr");
                uint size = 0;

                try
                {
                    size = s.FindUint("size");
                }
                catch (OverflowException)
                {
                    //TODO: sometimes for iOS, size is being reported as a number larger than uint
                    //TODO: currnetly, this is only superficial information displayed in the UI.
                    //TODO: so just swallow the exception and show zero. 
                    //TODO: this may be a bug in the lldb side
                }


                if (addr != INVALID_ADDRESS)
                {
                    sections.Add(new Section(name, addr, size));
                }
            }
            if (sections.Count > 0)
            {
                Sections = sections;
            }
        }

        private class Section
        {
            public readonly string Name;
            public ulong BaseAddress;
            public ulong Size;

            public Section(string name, ulong baseAddr, ulong size)
            {
                Name = name;
                BaseAddress = baseAddr;
                Size = size;
            }
        }

        public bool AddressInModule(ulong address)
        {
            return Sections.Find((s) => s.BaseAddress <= address && address < s.BaseAddress + s.Size) != null;
        }

        private Section TextSection
        {
            get
            {
                if (Sections.Count > 1)
                {
                    Section t = Sections.Find((s) => s.Name == "__TEXT");
                    if (t != null)
                    {
                        return t;
                    }
                }
                return Sections[0];
            }
        }

        private List<Section> Sections { get; set; }
        public string Id { get; private set; }
        public string Name { get; private set; }
        public ulong BaseAddress { get { return TextSection.BaseAddress; } }
        public ulong Size { get { return TextSection.Size; } }
        public bool SymbolsLoaded { get; private set; }
        public string SymbolPath { get; private set; }

        public uint GetLoadOrder() { return _loadOrder; }

        public Object Client { get; set; }      // really AD7Module
    }
}
