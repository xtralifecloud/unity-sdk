Known issues {#known_issues_ref}
===========

There are a few use cases that we know to be causing issues, which are usually due to broken functionality in Unity. If you are experiencing strange behaviour, please check out the following list first.

## HTTP request freezing on second run from the editor

This bug has been reproduced on Unity 4 on MS Windows (8.1). It is currently unknown whether it affects subsequent or previous versions of either the operating system or Unity software, but it does not affect the Mac OS X build of Unity 5 at least.

Any request (such as login) will freeze upon second run of a scene just after having launched Unity. Retrying the request will then work immediately. Subsequent requests and runs of the scene also work. In order to reproduce the issue, you need to quit Unity, launch the scene, stop it, then launch it again; the first request will then time out.

This bug has been reported in the official bug tracker at Unity.
