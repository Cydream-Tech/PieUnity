# NPBehave Vendor Copy

This directory contains a source vendor copy of NPBehave runtime scripts.

- Upstream: https://github.com/meniku/NPBehave
- Imported commit: `a1bc9673823610ea19198fc3e7268f8da8910cd4`
- License: MIT, preserved in `LICENSE.NPBehave.txt`

pie-unity intentionally vendors the source instead of depending on future upstream package updates. Do not update this copy casually; behavior-tree integration depends on the current public `NPBehave` namespace and runtime node semantics.
