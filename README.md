# Pretty Goog Character Controller

Simple overview of use/purpose.

## Description

This is a character controller for Unity that covers many edge cases where the built in character controller may fail.

## Unique Features

### Custom Physics Update

This project uses a Physics Manager script that tells physics when to update.  Usually physics is run at fixed time steps that are not the same as the games time steps.  This results in visual discrepencies in the movement of physics based objects and script updated objects.  Also if your game is running slowly, the physics may update multiple tiems per frame, which contributes to even more slowdown and even more physics updates per frame.  The Physics Manager script updates the physics at the begining of the frame using Time.deltaTime as the time step.  There are options to run multiple updates and slice the delta time it the frame rate gets low and options to set the max number of slices.  There is also options to trade multiple physics updates for an old school game slowdown.

### Running In Update

Usually it is advised to update physics objects in FixedUpdate so that the physics update can fix any issues you may have created.  A problem with this is that FixedUpdate runs before the physics update and what you end up with is the raw output of the physics.  What the character controller does instead is add corrections to the physics like moving the character up steps and applying world motion.  It tries its best to stop the character from being moved through any collision when making these corrections.  The only physics change is to the rigitbody velocity that does not change the position until th physics update on the next frame.

## License

This project is licensed under the MIT License - see the LICENSE.md file for details
