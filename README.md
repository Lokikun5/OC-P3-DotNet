# OC P3 — Validation et tests d’une fonctionnalité .NET

Projet réalisé dans le cadre du parcours **Développeur Back-End .NET** d’OpenClassrooms : *Testez l’implémentation d’une nouvelle fonctionnalité .NET*.

L’application permet de consulter un catalogue, de gérer un panier et de passer une commande. Un module d’administration permet de créer et supprimer des produits. Le travail présenté porte sur la validation du formulaire de création et ses tests unitaires.

## Technologies

- C# et ASP.NET Core MVC — cible `net6.0`
- Razor pour les vues
- Entity Framework Core et SQL Server
- ASP.NET Core Identity pour l’authentification
- Ressources `.resx` pour la localisation
- xUnit et Moq pour les tests unitaires

## Organisation du dépôt

Les projets se trouvent dans le dossier `DotNetEnglishP3-master` :

| Projet | Contenu |
| --- | --- |
| `P3AddNewFunctionalityDotNetCore` | Application Web, contrôleurs, vues, services, repositories et ressources |
| `P3AddNewFunctionalityDotNetCore.Tests` | Tests unitaires de `ProductService` |

Le code applicatif et les noms des tests sont en anglais.

## Installation et lancement

### Prérequis

- SDK .NET 6 pour restaurer, compiler et tester la solution.
- Visual Studio avec la charge de travail de développement Web ASP.NET, pour le lancement depuis l’IDE.
- Une instance SQL Server locale, par exemple SQL Server Express.
- SQL Server Management Studio (SSMS), pour consulter les données et vérifier la connexion. SSMS est un outil d’administration ; le moteur SQL Server doit être installé séparément.

### Récupérer le projet

```powershell
git clone https://github.com/Lokikun5/OC-P3-DotNet.git
cd OC-P3-DotNet/DotNetEnglishP3-master
```

Ouvrir la solution dans Visual Studio.

### Configurer les bases de données

Adapter la section `ConnectionStrings` du fichier `P3AddNewFunctionalityDotNetCore/appsettings.json` à l’instance SQL Server locale.

Exemple pour une instance `SQLEXPRESS`, avec authentification Windows :

```json
"ConnectionStrings": {
  "P3Referential": "Server=.\\SQLEXPRESS;Database=P3Referential-2f561d3b-493f-46fd-83c9-6e2643e7bd0a;Trusted_Connection=True;MultipleActiveResultSets=true",
  "P3Identity": "Server=.\\SQLEXPRESS;Database=Identity;Trusted_Connection=True;MultipleActiveResultSets=true"
}
```

Le double antislash est nécessaire dans une chaîne JSON. Adapter le nom d’instance si SQL Server utilise un autre nom.

- **P3Referential** : données de l’application.
- **P3Identity** : données d’authentification des utilisateurs.

Le projet prévoit la création des bases au premier lancement. Le service SQL Server doit être démarré et le compte utilisé doit disposer des droits nécessaires. Vérifier la connexion dans SSMS en cas d’erreur.

Ne pas publier de mots de passe ou de chaînes de connexion contenant des secrets.

### Démarrer l’application

1. Définir `P3AddNewFunctionalityDotNetCore` comme projet de démarrage.
2. Exécuter **Générer → Générer la solution**.
3. Lancer avec **F5**.
4. Consulter le catalogue, puis accéder au module d’administration avec un compte configuré dans l’environnement local.

L’adresse et le port dépendent du profil de lancement sélectionné. Les actions d’administration présentées nécessitent une authentification.

## Validation de la création d’un produit

La méthode `ProductService.CheckProductModelErrors` reçoit un `ProductViewModel` et retourne une liste de messages d’erreur localisés.

| Champ | Règle | Clé du message |
| --- | --- | --- |
| Nom | Obligatoire ; une valeur vide ou composée uniquement d’espaces est refusée | `MissingName` |
| Prix | Obligatoire | `MissingPrice` |
| Prix | Nombre fini ; texte invalide, `NaN` et infinis refusés | `PriceNotANumber` |
| Prix | Strictement supérieur à zéro | `PriceNotGreaterThanZero` |
| Stock | Obligatoire | `MissingStock` |
| Stock | Nombre entier | `StockNotAnInteger` |
| Stock | Strictement supérieur à zéro | `StockNotGreaterThanZero` |

Les champs `Description` et `Details` peuvent rester vides. Les vérifications successives produisent au maximum une erreur métier par champ.

Dans l’action POST `ProductController.Create`, les erreurs retournées sont ajoutées au `ModelState`. Si celui-ci est invalide, le formulaire est réaffiché avec les valeurs saisies et les messages. L’enregistrement n’est effectué que lorsque `ModelState.IsValid` vaut `true`, puis l’utilisateur est redirigé vers la liste d’administration.

La lecture du prix dépend de `CultureInfo.CurrentCulture` : les tests couvrent notamment `12,50` en français et espagnol, et `12.50` en anglais.

## Tests unitaires

Les tests sont regroupés dans `ProductServiceTests.cs` et organisés selon **Arrange, Act, Assert**. Les dépendances du service sont simulées avec Moq : ces tests ne nécessitent ni navigateur ni connexion SQL Server.

| Groupe | Cas couverts | Nombre |
| --- | --- | ---: |
| Nom | `null`, vide, espaces | 3 |
| Produit valide | Aucune erreur attendue | 1 |
| Prix | Valeurs manquantes, texte, zéro, négatif, décimaux selon trois cultures, `NaN` et infinis | 13 |
| Quantité | Valeurs manquantes, texte, décimaux, zéro, négatif et entiers positifs | 10 |
| **Total** | | **27** |

**Dernière exécution globale vérifiée : 6 novembre 2026 — 27 tests réussis, 0 échec, 0 ignoré.**

Dans Visual Studio, arrêter l’application si elle est en cours d’exécution, puis sélectionner **Test → Exécuter tous les tests**. Les résultats sont disponibles dans l’Explorateur de tests.

Depuis le dossier `DotNetEnglishP3-master`, il est également possible de lancer :

```powershell
dotnet test ./P3AddNewFunctionalityDotNetCore.Tests/P3AddNewFunctionalityDotNetCore.Tests.csproj
```

Le localizer simulé renvoie les clés de ressources : les tests unitaires vérifient le choix des erreurs, pas leurs traductions réelles. Les messages du prix ont également été vérifiés manuellement en français et en anglais.

## Limites et anomalies suivies

- Le changement de langue vers l’espagnol ne fonctionne pas dans l’interface lors des essais réalisés. Les formats numériques sous `es-ES` passent en test unitaire, mais les messages espagnols restent à vérifier dans l’application.
- La gestion de l’inventaire comporte des anomalies observées lors de l’étude : absence de contrôle de disponibilité à l’ajout au panier et absence de sauvegarde explicite dans la branche qui supprime un produit lorsque son stock atteint zéro.
- Le calcul du total du panier ne tient pas compte des quantités dans le code étudié.

Ces points ne sont pas corrigés par les tests de validation du formulaire. Le succès des 27 tests concerne les scénarios de validation décrits ci-dessus.

## Origine

Application de départ fournie par [OpenClassrooms Student Center](https://github.com/OpenClassrooms-Student-Center/Back-End.NET_Testez_implementation_nouvelle_fonctionnalite_P3).

Travail de formation réalisé par **Antonio Quadjovie**.
