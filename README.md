# 3D Star Chart

## About the Project

This project is a 3D Star Chart application developed using C#, .NET 10 and OpenTK for my Advanced Software Engineering module.

The application displays stars in a three-dimensional space using information from a star catalogue. Instead of displaying stars on a flat map, the application uses their X, Y and Z coordinates to position them in a 3D environment.

Users can explore the star chart by moving and rotating the camera, selecting different stars, searching for a particular star and travelling towards a selected destination.

I have also included star colours based on spectral information, labels for named stars, distance and direction calculations, and an animated dust effect during travel.

The application runs as a desktop window, with keyboard and mouse controls used for navigation.

## Technologies Used

The project uses the following technologies:

- **C#:** The main programming language used to develop the application.
- **.NET 10:** Used to build and run the project.
- **OpenTK:** Used to create the application window, handle keyboard and mouse input, and render the 3D star chart using OpenGL.
- **OpenGL:** Used for drawing stars, labels, destination markers and dust particles.
- **GLSL shaders:** Used to control how stars and other objects are displayed.
- **Git and GitHub:** Used for version control and managing development through feature branches and pull requests.
- **Visual Studio Code:** Used for writing, editing, building and testing the code.

## Star Data

The application uses a star catalogue containing information about stars and their positions.

The data includes three-dimensional coordinates and spectral information used by the renderer.

When the application starts, the catalogue is loaded and the stars are prepared for rendering.

In my current version, the application loads **119,626 catalogue stars**, along with one additional custom star named **Rojani**, giving a total of **119,627 stars**.

### Star Coordinates

Each star has three coordinates:

- **X:** Position along the horizontal X axis.
- **Y:** Position along the vertical Y axis.
- **Z:** Position along the depth axis.

These coordinates are used to place stars in 3D space.

The camera also has an X, Y and Z position. When the camera moves, the view of the stars changes based on their positions relative to the camera.

The distances displayed by the application are measured in parsecs and converted into light-years.

One parsec is approximately 3.26156 light-years.

### Star Colours

The application uses spectral information to give stars different colours.

For example, some stars appear blue or blue-white, while others appear white, yellow, orange or red.

This helps distinguish stars visually instead of displaying every star as a white point.

The star colours are drawn using the OpenGL rendering system.

### Named Stars

The application includes a selection of named stars that users can navigate to.

Some examples are:

- Sirius
- Vega
- Altair
- Procyon
- Arcturus
- Rigel
- Betelgeuse
- Capella
- Deneb
- Polaris

There is also a custom star called Rojani.

The named-star mode makes it easier to find and select stars without displaying the full catalogue.

## Main Features

### 1. 3D Star Rendering

The main feature of the application is the 3D star chart.

When the program starts, it loads the star data and displays the stars in a dark space environment.

The stars are drawn as points using OpenGL.

Their positions are calculated from the coordinates in the star data, and their appearance is controlled by shaders.

The renderer uses buffers to store the star information, rather than manually drawing every star individually.

This allows the application to handle a large number of stars.

### 2. Camera Movement

The application includes a camera that allows users to explore the star chart.

The camera starts at the home position:

- X = 0
- Y = 0
- Z = 0

Users can move forwards, backwards, left, right, upwards and downwards.

The camera can also rotate to look in different directions.

The movement is calculated using the camera's position, forward direction and orientation.

### 3. Camera Rotation

The arrow keys are used to rotate the camera.

The left and right arrow keys change the horizontal viewing direction.

The up and down arrow keys change the vertical viewing direction.

The camera can also be rotated using the mouse by holding the right mouse button and dragging.

This allows users to look around the star chart without changing their position.

### 4. Zoom

Users can zoom using the mouse wheel or trackpad.

Zoom moves the camera forwards or backwards along its viewing direction, allowing users to explore different areas of the star chart.

### 5. Adjustable Movement Speed

The camera movement speed can be changed while the application is running.

Pressing `+` increases the movement speed, while pressing `-` decreases it.

This is useful because stars can be located at very different distances.

The application displays the updated camera speed in the terminal.

### 6. Named-Star Filter

The application has two viewing modes.

**Full catalogue mode**

This displays all loaded stars, including the custom star.

**Named-star mode**

This displays a smaller selection of 50 named stars.

Users can switch between these modes by pressing `F`.

This feature makes the application easier to navigate when the full star chart becomes crowded.

### 7. Star Labels

The application displays labels for selected named stars.

The labels are rendered within the OpenGL window rather than using a separate interface.

A custom pixel-style font is used to draw the names.

The label renderer also checks the screen positions of labels to reduce overlapping text.

Not every label is guaranteed to be visible at the same time. Labels may be hidden when stars are outside the camera view or when there is not enough space to display them clearly.

### 8. Star Selection

Users can select a star by clicking its visible name or point.

The application checks whether the mouse position is close enough to a selectable star.

If a matching star is found, it becomes the selected destination.

The application displays information about the star in the terminal and starts travelling towards it.

If no selectable star is found, the application displays a message explaining that no star was selected.

### 9. Destination Navigation

The application includes a destination list containing named stars.

Users can move through the list using:

- `N` — Select the next destination.
- `P` — Select the previous destination.

When a destination is selected, the camera turns towards the star.

The terminal displays information about the selected destination, including its coordinates and distance.

### 10. Star Search

Users can search for a star by its name.

To search:

1. Press `/` to open search mode.
2. Type the name of the star.
3. Press `Enter` to search.
4. If the star is found, it becomes the selected destination.
5. Press `Enter` again to start travelling.

The search also supports matching the beginning of a star's name.

If the application cannot find a matching star, it displays a message.

Pressing `Backspace` removes characters while typing, and `Escape` cancels the search.

The search text is displayed in the application window title.

### 11. Animated Travel

One of the main navigation features is animated travel between stars.

After selecting a destination, pressing `Enter` starts the journey.

Instead of moving the camera instantly to the destination, the application gradually changes the camera position.

The journey currently takes approximately six seconds.

The movement uses interpolation to create a smoother transition.

The camera stops a short distance away from the selected star rather than moving directly onto its position.

During the journey, the terminal displays progress updates:

- 0%
- 25%
- 50%
- 75%
- 100%

Once the journey finishes, the application displays an arrival message and the camera's new coordinates.

### 12. Space-Dust Animation

I added a space-dust animation to make travelling between stars more noticeable.

The dust is made from small blue particles rendered using OpenGL.

The particles are generated separately from the star catalogue and are used only as a visual effect.

The animation works as follows:

1. The user selects a destination.
2. The user starts travelling.
3. The dust particles become visible.
4. The particles move while the journey is active.
5. When the journey finishes, the dust disappears.

The dust animation is not intended to represent real astronomical dust measurements. It is a visual effect that makes the travel animation easier to recognise.

In the current implementation, the dust moves along the Z axis rather than automatically matching every possible travel direction.

### 13. Distance, Bearing and Elevation

When a star is selected, the application calculates its position relative to the camera.

**Distance**

The distance is calculated from the difference between the camera position and the star coordinates.

The result is displayed in parsecs and light-years.

**Bearing**

Bearing describes the horizontal direction towards the selected star.

In this application's coordinate system, a bearing of 0° points along the negative Z axis.

**Elevation**

Elevation describes how far above or below the horizontal plane the selected star is located.

The Y axis is used as the vertical axis.

These calculations are updated as the camera moves.

### 14. Cancelling a Journey

Pressing `X` cancels an active journey or camera-turning animation.

The camera remains at its current position instead of returning automatically to the starting point.

This allows the user to stop travelling and continue exploring.

### 15. Returning Home

Pressing `R` returns the camera to its original position and orientation.

This is useful if the user travels far away or becomes unsure of their location in the star chart.

The home position is X = 0, Y = 0, Z = 0.

### 16. Camera Coordinates

Pressing `C` displays the camera's current coordinates in the terminal.

This is useful for checking movement and understanding the camera's position in 3D space.

## Controls

| Key or Action | Function |
|---|---|
| `W` | Move forwards |
| `S` | Move backwards |
| `A` | Move left |
| `D` | Move right |
| `Shift` | Move upwards |
| `Space` | Move downwards |
| `Left Arrow` | Rotate left |
| `Right Arrow` | Rotate right |
| `Up Arrow` | Rotate upwards |
| `Down Arrow` | Rotate downwards |
| Right mouse drag | Rotate the camera |
| Mouse wheel / trackpad scroll | Zoom forwards or backwards |
| `+` | Increase movement speed |
| `-` | Decrease movement speed |
| `F` | Switch between full catalogue and 50 named stars |
| `N` | Select next destination |
| `P` | Select previous destination |
| `/` | Search for a star |
| `Enter` | Confirm search or travel to selected star |
| Left mouse click | Select a star and start travelling |
| `X` | Cancel an active journey |
| `R` | Return home |
| `C` | Display camera coordinates |
| `Escape` | Cancel search or close the application |

## Project Structure

The application is divided into different C# files to keep the code organised.

### Program.cs

This is the entry point of the application.

It starts the program, loads the required data and creates the main application window.

### Star.cs

This file represents an individual star.

It stores information about a star, such as its name and coordinates.

### StarCollection.cs

This file manages the collection of stars.

It is responsible for handling the star data used throughout the application.

### StarRenderer.cs

This file handles the rendering of stars.

It uses OpenGL shaders and buffers to display star positions and colours.

It also handles switching between the full catalogue and the named-star selection.

### StarChartWindow.cs

This is the main application window.

It connects the different parts of the project and handles the main application events.

Its responsibilities include:

- Initialising the renderers.
- Handling keyboard and mouse input.
- Updating the camera.
- Managing star selection.
- Starting and updating travel animations.
- Updating the application title.
- Drawing the scene.
- Cleaning up OpenGL resources when the application closes.

### CameraController.cs

This file manages the camera.

It contains the camera position, movement speed, rotation and zoom calculations.

Keeping the camera logic in a separate class makes it easier to maintain.

### StarNavigation.cs

This file manages destination selection and searching.

It keeps track of the selected star and provides methods for moving through destinations and finding stars by name.

It also displays information about selected stars.

### StarPicker.cs

This file handles selecting stars using the mouse.

It uses the projected screen positions of stars to determine whether the user clicked near a selectable star.

### StarLabelRenderer.cs

This file draws star names in the application window.

It uses a custom pixel font and checks label positions to reduce overlapping text.

### DestinationRenderer.cs

This file draws a visual marker for the selected destination.

It helps users identify the star they are currently navigating towards.

### DustRenderer.cs

This file manages the animated dust particles.

It creates the particle positions, updates their movement and renders them using OpenGL.

The main application controls when the dust is displayed.

## How to Run the Project

### Requirements

Before running the application, make sure you have:

- The .NET 10 SDK installed.
- Visual Studio Code or another editor that supports C#.
- A computer that supports the OpenGL version required by the application.
- The complete project files, including the star catalogue data used by the program.

The project uses OpenTK, which is managed through the .NET project dependencies.

### Step 1: Get the Project

Clone the GitHub repository:

```bash
git clone https://github.com/rojaniiii/Advance-software-engineering.git
```

Open the downloaded project folder in Visual Studio Code.

### Step 2: Open the Terminal

In Visual Studio Code, open:

**Terminal → New Terminal**

Navigate to the folder containing `StarChart.csproj`.

### Step 3: Restore Dependencies

Run:

```bash
dotnet restore
```

This restores the packages required by the project.

### Step 4: Build the Project

Run:

```bash
dotnet build
```

If everything is set up correctly, the terminal should display:

```text
Build succeeded.
```

### Step 5: Run the Application

Run:

```bash
dotnet run
```

A new window should open displaying the 3D star chart.

The terminal will also display the application controls and information about the loaded stars.

Keep the terminal open while using the application because it displays navigation messages and travel progress.

## Example: Travelling to a Star

Here is an example of how to use the application.

1. Run the application using `dotnet run`.
2. Press `F` if you want to display only the named stars.
3. Press `/` to open the search.
4. Type `Sirius`.
5. Press `Enter` to search.
6. The application selects Sirius and turns towards it.
7. Press `Enter` again to start travelling.
8. Watch the camera move towards the star while the dust particles appear.
9. Wait for the journey to reach 100%.
10. Press `C` to view your new camera position.
11. Press `R` to return home.

You can repeat these steps with other named stars.

## Testing

I tested the application during development by building and running it from the terminal.

The main checks included:

| Test | Result |
|---|---|
| Building the project using `dotnet build` | Passed |
| Loading the star catalogue | Passed |
| Displaying spectral star colours | Passed |
| Switching between full and named-star modes | Passed |
| Selecting stars using the mouse | Passed |
| Searching for stars | Passed |
| Travelling to selected destinations | Passed |
| Displaying travel progress | Passed |
| Displaying dust during travel | Passed |
| Returning to the home position | Passed |

The latest build completed successfully without errors.

The project was also committed and pushed to GitHub using Git.

## Current Limitations

Although the main features are implemented, there are some limitations.

**Star density**

When the full catalogue is displayed, some areas contain many stars close together on the screen. This can make individual stars difficult to distinguish.

**Star labels**

The application attempts to reduce overlapping labels, but some labels may be hidden when there is not enough space.

**Dust animation**

The dust is a visual effect rather than a scientific simulation. The current particles move along a fixed axis.

**Star selection**

Mouse selection is based on the stars available for selection in the current mode. Clicking an arbitrary unnamed catalogue star does not necessarily select it.

**Travel**

The travel animation uses a fixed duration and smooth camera movement. It is intended for exploring the application and does not represent realistic spacecraft travel speeds.

## Version Control

Git and GitHub were used to manage the development of the application.

Different features were developed on separate branches so that changes could be worked on and tested before merging them into the main branch.

For example, the `feature4-camera-controls` branch contains work on camera movement, navigation and travel-related functionality.

Changes were committed and pushed to GitHub, and pull requests were used to review and merge feature branches.

Repository:

https://github.com/rojaniiii/Advance-software-engineering

## Conclusion

This project allowed me to work with 3D graphics, OpenGL rendering, camera movement and user interaction in C#.

I started with displaying stars from a catalogue and then added features to make the application more interactive, including camera controls, searching, navigation and animated travel.

One of the more challenging parts was making the different features work together, especially camera movement, star selection and rendering. I also had to test and adjust the star colours, labels and navigation calculations.

The final application allows users to explore a large star catalogue in a 3D environment and travel between selected stars using the controls provided.