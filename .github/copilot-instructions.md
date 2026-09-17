# Workspace AI Agent Rules

## Definition of Done:
Wanneer een gevraagde feature of bugfix succesvol is geïmplementeerd en geverifieerd (via builds/logs/tests):
1. Wacht niet op handmatige bevestiging voor versiebeheer.
2. Voer zelfstandig `git add .` uit.
3. Maak een duidelijke commit aan volgens Conventional Commits (bijv. `feat: ...`, `fix: ...`).
4. Zorg dat de wijzigingen op `main` terechtkomen en voer direct `git push origin main` uit.
5. Rapporteer kort aan de gebruiker wat er is afgerond en gepusht.
