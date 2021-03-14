![chx](https://user-images.githubusercontent.com/3764951/110683736-5b67b200-8202-11eb-8801-1ba969f6714c.gif)


Installation:
-
- Note: This plugin only works with the Universal Render Pipeline at the moment.
- If you have a git account, add this project as a dependency in `Packages/manifest.json` in the root your your project
`"com.opeious.pokemon3dstounity": "https://github.com/opeious/Pokemon3DStoUnity.git",`

- If you don't have git setup, download and put the entire project into your Assets Folder

Usage:
-
![2021-02-27 22_53_24-Pokemon3DStoUnity - SampleScene - PC, Mac   Linux Standalone - Unity 2019 4 18f1](https://user-images.githubusercontent.com/3764951/109395128-c8509180-7950-11eb-8b5b-2243dcf1f899.png)

- From the top bar click 3DStoUnity and hit import
- Place your 3DS files in `Assets/Bin3DS` that would've been created
- Hit import again
- Exported files and prefabs get added to  `Assets/Exported`

Contributions:
-
If you would like to contribute to this project, I would recommend going through the TODOs at the top of `PokemonImporter.cs` in the `main` branch:
https://github.com/opeious/Pokemon3DStoUnity/blob/main/Editor/PokemonImporter.cs


Settings to change for Fire to work:
-
![2021-03-11 00_01_26-NVIDIA GeForce Overlay](https://user-images.githubusercontent.com/3764951/110681966-69b4ce80-8200-11eb-9334-269ffc47ba27.png)
- Add two new layers called "FireCore" and "FireMask"

- Either create a new ForwardRenderer (sample attached in root of this repo), or select your existing ForwardRenderer
- The default ForwardRenderer will be with URP settings in `Assets/Setting`
- In the ForwardRenderer: From Opaque Layer Mask remove FireCore and FireMask.
- Add two render objects one for each layer, for their individual settings refer the image below
- In your project settings. (URP Renderer Settings) If you created a new ForwardRenderer, set the new one as default

![2021-03-10 23_58_49-NVIDIA GeForce Overlay](https://user-images.githubusercontent.com/3764951/110682434-ec3d8e00-8200-11eb-977a-b309efb81cce.png)


What does the package do at the moment:
-
- Open the Binary using SPICA's H3D parser
- Translate the 3D model to Unity Mesh system and skinned mesh renderers
- Generate material files from the textures 
- Automatically copy over some of the shader settings to the newly created materials
- Skins and generates the skeleton for the mesh
- Generates skeletal animations from the binaries
- Generate material / vis animations and append them to skeletal animations
- Renames animations, skips repeated animations
- Saves the translated mon as prefab
- Supports bulk processing

Updating:
-
- Unity package manager doesn't currently support version of git packages, for now just remove the project as a dependency and add it again

ChangeLog:
-
- v1.8: Fixes, delete settings to regenerate defaults, custom shader support for material animations
- v1.7: Fire Shader
- v1.6: Full material / vis animation support, bunch of fixes related to animations and skipping corrupt files etc.
- v1.5: Added Visiblity animations, fixed iris/body shaders, Material aniamtions (preview)
- v1.4: Added UI, made the plugin customizable, animation renaming, etc
- v1.3: Skeletal animation generation
- v1.2: Skinning and rigging fixes
- v1.1: sample toon shader, asset creation
- v1: basic skeleton, model, texture generation

![2021-02-27 22_43_19-NVIDIA GeForce Overlay](https://user-images.githubusercontent.com/3764951/109395153-e4543300-7950-11eb-8351-e42af713c374.png)
![2021-02-27 23_07_57-PokeUnity - SampleScene - PC, Mac   Linux Standalone - Unity 2019 4 8f1 Personal](https://user-images.githubusercontent.com/3764951/109395156-e918e700-7950-11eb-83c9-4923417450f1.png)![syl2](https://user-images.githubusercontent.com/3764951/110213468-330c4a80-7ec6-11eb-8f94-02aa35e54abc.gif)



