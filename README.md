What does the package do at the moment:
-
- Open the Binary in SPICA
- Translate the 3D model to Unity Mesh system and skinned mesh renderers
- Generate material files from the textures 
- Automatically copy over some of the shader settings to the newly created materials
- Saves the translated mon as prefab
- Supports bulk processing

To do:
-
- Normal/Occlusion shaded materials
- Auto creating texture atlas for each mon
- Fixing the skeleton rotation issue for some mons
- Translating the animation binaries to Unity clips and controllers
- Material animations

ChangeLog:
-
- v1.1: sample toon shader, asset creation
- v1: basic skeleton, model, texture generation

![2021-02-27 22_43_19-NVIDIA GeForce Overlay](https://user-images.githubusercontent.com/3764951/109395153-e4543300-7950-11eb-8351-e42af713c374.png)
![2021-02-27 23_07_57-PokeUnity - SampleScene - PC, Mac   Linux Standalone - Unity 2019 4 8f1 Personal](https://user-images.githubusercontent.com/3764951/109395156-e918e700-7950-11eb-83c9-4923417450f1.png)


Usage:
-
![2021-02-27 22_53_24-Pokemon3DStoUnity - SampleScene - PC, Mac   Linux Standalone - Unity 2019 4 18f1](https://user-images.githubusercontent.com/3764951/109395128-c8509180-7950-11eb-8b5b-2243dcf1f899.png)

- From the top bar click 3DStoUnity and hit import
- Place your 3DS files in `Assets/Bin3DS` that would've been created
- Hit import again
- Exported files and prefabs get added to  `Assets/Exported`

Installation:
-
- If you have a git account, add this project as a dependency in `Packages/manifest.json`  
`"com.opeious.pokemon3dstounity": "https://github.com/opeious/Pokemon3DStoUnity.git",`

- If you don't have git setup, download and put the entire project into your Assets Folder

Updating:
-
- Unity package manager doesn't currently support version of git packages, for now just remove the project as a dependency and add it again
