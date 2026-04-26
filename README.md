# LifeSimulation
Extended Life Simulation

A Life Simulation Ecosystem Simulation Capstone Project


Overview
This project is an interactive life simulation inspired by foundational systems such as Conway’s Game of Life and modern biological simulations. It models an evolving ecosystem composed of multiple life forms interacting under dynamic environmental conditions.

At its core, the simulation explores:
Ecosystem dynamics (predation, growth, extinction)
Population stability and collapse
Emergent behavior from simple rules
User-driven experimentation with environmental parameters

The system allows users to create, observe, and evaluate simulated ecosystems, making it both a technical and exploratory tool.


Simulation Concept
The simulation models three primary categories of life:
Plants — resource producers
Grazers — consume plants for survival
Predators — hunt grazers

Each category operates under rules governing:
Growth and reproduction
Resource consumption
Survival constraints

The interaction of these systems produces emergent ecological behavior, including:
Population booms and crashes
Extinction events
Resource scarcity cycles


Project Goals (Based on Capstone Requirements)
This project was developed to satisfy a tiered set of requirements:
Tier 1 — Baseline Simulation
Multiple life form categories with core behaviors
Real-time, top-down simulation display
Adjustable simulation speed
Configurable starting conditions
Simulation summary after each run
Discrete time progression
Tier 2 — Interactivity & Evaluation
Terrain affecting simulation behavior
Interaction reporting (predation, movement, etc.)
Scoring system for comparing runs
Leaderboard for tracking performance
Tier 3 — Diversity & Behavior States
Distinct life states (e.g., searching, feeding, fleeing)
Variation within species (trait differences)
Expanded behavioral complexity
Optional visual editor for simulation setup
Tier 4 — Advanced Systems (Partial / Extensible)
Foundations for evolutionary traits and ecosystem complexity
Expandable system for future biological realism


Scene Structure
The application is organized into multiple scenes, each with a clear responsibility:
Main Menu - Entry point and navigation
Configuration - Define simulation parameters before running
Simulation - Core ecosystem simulation and live controls
Score Summary - Displays results and statistics after a run
Leaderboard - Compares simulation outcomes
Credits - Attribution and acknowledgments


User Flow
Main Menu
   ↓
Configuration
   ↓
Simulation (run + observe)
   ↓
Score Summary
   ↓
Leaderboard
   ↓
Main Menu


Simulation Features
Real-Time Ecosystem
Continuous updates using discrete time steps
Visual feedback for all life forms
Immediate response to environmental changes

Configurable Parameters
Users can adjust:
Population sizes
Growth rates
Resource availability
Behavioral tendencies

This enables experimentation with different ecological scenarios.

Run Summary & Analytics
After each simulation:
Starting, peak, and ending populations are recorded
Simulation duration is tracked
Key events may be logged (e.g., extinction, overpopulation)

Leaderboard System
Compares outcomes across runs
Encourages optimization and experimentation
Provides a competitive or analytical dimension

Simulation Scene (Core System)
The Simulation scene is the heart of the project.

It includes:
Live ecosystem rendering
Runtime controls (pause, adjust, configure)
Access to simulation settings


Simulation Control
Pause / Resume
Users can freeze and resume the simulation at any time
Allows detailed observation of system behavior
Map Size Presets
Small / Default / Large environments
Directly influence population capacity and interaction density
Live Settings Editor
Modify simulation parameters while running
Immediate effect on ecosystem behavior


Settings System
The simulation includes a categorized settings interface:
Game — simulation speed, terrain generation
Plant — growth, replenishment, population limits
Grazer — feeding, reproduction, traits
Predator — hunting behavior, population limits

All changes are applied in real time, enabling rapid iteration and experimentation.

User Experience Design
The project follows strict UX constraints:
Responsive — minimal lag or delay
Visually clear — consistent and readable UI
Intuitive — minimal learning curve
Engaging — encourages exploration

Design decisions prioritize:
Immediate feedback
Clear visual hierarchy
Minimal friction for the user

Key Design Principles
1. Emergent Behavior
Simple rules lead to complex, unpredictable outcomes.

2. Player-Driven Experimentation
Users are encouraged to:
Adjust variables
Observe outcomes
Compare results

3. Separation of Systems
Simulation logic
UI interaction
Data storage
Each operates independently for maintainability.

4. Multi-Scene Organization
Each scene has a focused role, reducing complexity and improving clarity.

Extensibility
The system is designed to support future expansion:
Additional species or subtypes
Genetic traits and evolution systems
Environmental systems (weather, seasons)
More advanced scoring metrics


Summary
This project delivers a complete ecosystem simulation experience that combines:
Biological modeling
Real-time interaction
Data-driven experimentation
Multi-scene application structure

It demonstrates the ability to design and implement:
Complex system interactions
User-focused simulation tools
Scalable architecture for future growth


Build & Installation Instructions
Option 1 — Open in Unity
Install Unity Hub
Install Unity Editor version 6000.3.5f2
Clone or download this repository
Open Unity Hub → Click “Add Project”
Select the project folder
Open the project

Running the Simulation
Open the Main Menu scene:
Assets/Scenes/MainMenu.unity
Press Play in the Unity Editor
Navigate:
Main Menu → Configuration → Simulation

Option 2 — Prebuilt Executable (If Included)
If a build is provided:
Navigate to the /Build/ folder
Run:
ExtendedLifeSimulation.exe
No installation required

Controls (Simulation Scene)
WASD / Arrow Keys — Move camera
Mouse Scroll — Zoom in/out
UI Buttons — Control simulation, spawn entities, adjust settings

Dependencies
Unity Editor 6000.3.5f2
Universal Render Pipeline (URP)

Notes
First-time load may take a few seconds while assets initialize
Simulation begins only after generating a map
Performance may vary based on selected map size
