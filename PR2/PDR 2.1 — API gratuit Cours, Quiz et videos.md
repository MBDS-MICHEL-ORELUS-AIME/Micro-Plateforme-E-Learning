PDR 2.1 - Strategie API gratuite pour Cours, Quiz et Videos

1. Contexte
Pour un projet academique sans budget, l'objectif est d'alimenter la plateforme avec des contenus gratuits et legalement reutilisables, sans dependre d'API payantes.

2. Principe directeur
Prioriser dans cet ordre :
- API officielles gratuites
- Flux ouverts (RSS, exports publics)
- Import manuel assiste
- Scraping uniquement en dernier recours (et seulement si autorise)

3. Sources recommandees (faible risque)
- Wikiversity / Wikipedia API : contenus pedagogiques textuels, faciles a mapper vers Module/Lecon.
- Project Gutenberg : ressources du domaine public (langues, histoire, culture generale).
- OpenEdition (selon la licence de chaque contenu) : articles academiques en acces libre.
- Plateformes videos ouvertes (YouTube/PeerTube) : integration par URL externe plutot que telechargement local.

4. Conformite legale minimale
Avant tout import, verifier :
- Licence de reutilisation (CC, domaine public, etc.)
- Conditions d'utilisation de l'API ou du site
- Attribution obligatoire (auteur, source, lien original)
- Restrictions de redistribution

5. Strategie technique (ASP.NET MVC + PostgreSQL)
5.1 Pipeline d'import
- Etape 1 : recuperation (API/flux/fichier)
- Etape 2 : normalisation (titre, description, langue, niveau, tags)
- Etape 3 : mapping vers le modele interne
- Etape 4 : validation (doublons, longueur, format)
- Etape 5 : insertion en base

5.2 Mapping propose
- SourceCourse -> Module (Title, Description)
- SourceLesson -> Lesson (Title, TextContent, VideoUrl, PdfPath, Order)
- SourceQuiz -> Quiz + Question + Option (si disponible)

5.3 Tracabilite recommandee
Conserver la provenance (table ou champs dedies) :
- SourceName
- SourceUrl
- SourceLicense
- ImportedAt

6. Scraping : regle de prudence
Le scraping ne doit pas etre la methode par defaut.
Il est acceptable seulement si :
- aucune API n'est disponible,
- robots.txt et conditions du site l'autorisent,
- le rythme de collecte est limite (throttling),
- l'attribution est conservee.

7. Plan d'execution MVP
- Sprint 1 : import texte via Wikiversity/Wikipedia API -> Module + Lesson.
- Sprint 2 : integration videos externes (URL) + metadonnees de provenance.
- Sprint 3 : import quiz simples (QCM) + deduplication.
- Sprint 4 : tableau de bord de synchronisation (volumes, erreurs, derniere synchro).

8. Criteres d'acceptation
- Import d'au moins 20 modules sans erreur bloquante.
- 100% des contenus importes avec attribution source.
- Aucun contenu importe sans verification de licence.
- Synchronisation stable sur batch planifie.

9. Risques et mitigation
- Changement API externe -> versionner les adaptateurs d'import.
- Qualite heterogene des contenus -> revue enseignant avant publication.
- Doublons -> hash de controle (titre + source + url).

10. Conclusion
La meilleure approche est un pipeline "API-first" et "legal-first", aligne avec la stack ASP.NET MVC + PostgreSQL. Cela permet d'enrichir durablement la plateforme sans abonnement payant et avec un risque juridique maitrise.