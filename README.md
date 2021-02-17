# Asteroid
![](./.readme-assets/demo.gif)

.Net Realtime networked physics engine/demo "game". Made on top of Box2D physics simulation using UDP as a transport protocol.
I used monogame framework for input & graphics ~~and actually I regretted about it because of a bunch of rendering bugs
when using OpenGL as a rendering backend.~~ It meant to be used on a local network.

### Implementation
The core idea of the implementation is instead of state interpolation use synchronization of the participants.
If the same event/state change happens on the same frame on different simulations they will change their state
the same way (~~depending on Box2d precision configuration~~).
It allows to reduce package sizes and make simulation potentially infinitely complicated. But with such
pitfalls like higher latency(although on local network it may not be super crucial).

## Implemented features
 - UDP IPv4 datagram broadcasting for scanning network for game rooms
 - platform agnostic code (currently there is only desktop csproj but it's pretty easy 
    to add mobile version because all base code lives in Asteroid.Core)