# LogBookPatcher

The LogBook has (at least) two outstanding bugs.

* While entries are being generated, Equipment-based entries *do not use the EquipmentDef's prefab reference*, instead using the (now obsolete) direct prefab. As a result, equipment that uses only the newest system will not have a display.
* Entries that have a prefab reference, instead of a direct prefab, will null ref when switched to after looking at an entry that does *not* have a prefab reference.

This mod fixes both issues small IL hooks.