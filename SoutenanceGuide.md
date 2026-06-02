# Guide de soutenance - Micro-Plateforme E-Learning

## 1. Introduction

- Présentez le projet : une micro-plateforme d'apprentissage en ligne (`MicroLMS`) développée en ASP.NET Core 8.
- Objectif : proposer un site web permettant la gestion de modules, de leçons, de quiz, de progression et de certificats.
- Contexte : projet de semestre pour le PDR 2, avec PostgreSQL comme base de données.

## 2. Enjeux et objectifs

- Créer une application web responsive et fonctionnelle.
- Automatiser la création de la base de données et l'injection de contenu académique.
- Offrir une expérience d'apprentissage avec des modules, des leçons et des quiz.
- Intégrer des rôles (admin, enseignant, apprenant) et une logique de progression.

## 3. Technologies utilisées

- `ASP.NET Core 8`
- `Entity Framework Core`
- `PostgreSQL`
- `C#`
- `GitHub Copilot` pour assister l'implémentation et la rédaction du code.

## 4. Architecture du projet

- `E-learningProject.Core` : entités métiers (`User`, `Module`, `Lesson`, `Quiz`, `Question`, `Certificate`, etc.).
- `E-learningProject.Services` : services métiers (`QuizService`, `ProgressService`, `CertificateService`, `OpenContentImportService`).
- `E-learningProject.Web` : application web ASP.NET Core, contrôleurs, vues et configuration.
- `Program.cs` : configuration des services, connexion PostgreSQL, migrations et injection de données.

## 5. Fonctionnalités principales

- Gestion des modules et des leçons.
- Quiz associées aux modules avec plusieurs types de questions.
- Suivi de progression des apprenants.
- Génération de certificats.
- Import de contenu externe via un service HTTP.
- Authentification et gestion de rôles.

## 6. Configuration et exécution

### Prérequis

- `.NET SDK 8.0+`
- `PostgreSQL 14+`

### Démarrage local

1. Créer la base de données PostgreSQL :

```sql
CREATE DATABASE "MicroLmsDb";
```

2. Définir le mot de passe PostgreSQL en PowerShell :

```powershell
$env:POSTGRES_PASSWORD = "votre_mot_de_passe"
```

3. Lancer l'application :

```powershell
.\start-web.ps1
```

4. Accéder à l'application :

```text
http://localhost:5230
```

### Compte administrateur par défaut

- Email : `admin@elearning.local`
- Mot de passe : `Admin123`

## 7. Démonstration recommandée

### 7.1. Partie backend

- Montrez `Program.cs` : configuration de `DbContext`, injection de dépendances, migrations automatiques.
- Expliquez l'auto-seeding des données académiques (modules, leçons, quiz) au démarrage.

### 7.2. Partie frontend

- Ouvrez l'application dans le navigateur.
- Connectez-vous en tant qu'administrateur.
- Présentez la navigation entre les modules et les leçons.
- Lancez un quiz et montrez le résultat.
- Montrez le suivi de progression ou la génération de certificat si disponible.

### 7.3. Cas d'utilisation

- Exemple : un apprenant suit un module, consulte les leçons puis passe le quiz.
- Illustrez le flux complet de l'utilisateur.

## 8. Ce que Copilot a apporté

- Assistance pour écrire le code plus rapidement.
- Suggestions de structure et de logique métier.
- Aide à documenter les étapes et à formuler les explications.

> À mentionner : Copilot a été utilisé comme assistant de développement, mais c'est nous qui avons choisi et intégré les solutions dans le code.

## 9. Points forts du projet

- Architecture claire en couches.
- Intégration de PostgreSQL et EF Core.
- Démarrage automatique avec migrations et contenu de base.
- Fonctionnalités d'apprentissage complètes.

## 10. Limites et améliorations possibles

- Ajouter une interface utilisateur plus riche et responsive.
- Implémenter la gestion complète des rôles et permissions.
- Ajouter des statistiques détaillées de progression.
- Développer un import de contenu plus robuste.
- Mettre en place des tests unitaires et d'intégration supplémentaires.

## 11. Plan de présentation

1. Introduction du projet et des objectifs.
2. Architecture technique.
3. Démonstration live.
4. Explications des choix techniques.
5. Rôle de GitHub Copilot dans l'implémentation.
6. Difficultés rencontrées.
7. Améliorations futures.
8. Conclusion.

## 12. Conseils pour la soutenance

- Parlez clairement de votre rôle : conception, codage, validation.
- Montrez les résultats plutôt que d'entrer dans trop de détails.
- Soyez prêt à expliquer pourquoi vous avez choisi ASP.NET Core et PostgreSQL.
- Mentionnez que l'application peut démarrer automatiquement et qu'elle seed des données académiques.

---

Bonne soutenance !