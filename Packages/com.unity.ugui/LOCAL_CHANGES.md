# Local changes to Unity UI 2.6.0

This package is embedded to fix `Selectable` registration during Play Mode entry
in Unity 6000.6.0f1.

`ResetStaticsOnLoad` rebuilds the registry from components whose `OnEnable` has
already run, rather than discarding their registration while leaving their
`m_EnableCalled` flag set. This prevents `OnDisable` from indexing an empty array.
The change is Editor-only; Player builds retain the upstream implementation.

When updating Unity UI, check whether the upstream package fixes this lifecycle
issue before removing the embedded package.
