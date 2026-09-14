# Candidate UI fonts

Each subfolder contains a Google Fonts source TTF, its OFL license, and a saved TextMesh Pro `*-Regular SDF.asset`. The variable font sources for Oxanium, Exo 2, and Orbitron are also kept; their `Regular.ttf` files are fixed at weight 400 so Unity imports them as Regular rather than Thin.

| Pair | English / numbers | Korean |
| --- | --- | --- |
| 1 | oxanium/Oxanium-Regular SDF.asset | ibmplexsanskr/IBMPlexSansKR-Regular SDF.asset |
| 2 | rajdhani/Rajdhani-Regular SDF.asset | gothica1/GothicA1-Regular SDF.asset |
| 3 | exo2/Exo2-Regular SDF.asset | ibmplexsanskr/IBMPlexSansKR-Regular SDF.asset |
| 4 | chakrapetch/ChakraPetch-Regular SDF.asset | dohyeon/DoHyeon-Regular SDF.asset |
| 5 | orbitron/Orbitron-Regular SDF.asset | blackhansans/BlackHanSans-Regular SDF.asset |

These are selectable candidates. The active scene font assignments have not been changed. Use **Tools > Fonts > Create Candidate TMP Assets** in the Unity Editor if an asset needs to be regenerated.

To configure combinations, select `Assets/Resources/Fonts/GameFontSettings.asset` in Unity. **Body** is the default pair; **H1–H4** each hold an English and Korean TMP asset. The game chooses the English or Korean asset from the current interface language. Nicknames use the Korean asset so Hangul names display in either language.

To choose a combination for a particular TextMesh Pro text object, add the `ApplyFont` component to that object and select **Font Combination** (`Body`, `H1`, `H2`, `H3`, or `H4`) in its Inspector. The font updates in the editor and during Play mode. Text without this component uses Body when created by the existing UI code. H1–H4 are preset choices, not automatic font-size rules.
