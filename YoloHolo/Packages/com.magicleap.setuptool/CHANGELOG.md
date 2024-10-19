# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.0.13] - 2024-08-15
### Fixed
- Fixes Magic Leap root selection of sdk is installed on another drive

## [2.0.12] - 2024-07-31
### Fixed
- Fixes setting target architecture to no longer select 2 

## [2.0.11] - 2024-07-10
### Added
- Adds menu items for updating namespaces based on 2.2.0 API

## [2.0.10] - 2024-05-21
### Fixed
- Fixes issue with loading bar remaining after package is imported.

## [2.0.9] - 2024-05-02
### Fixed
- Fixes conflict with partial classes.

## [2.0.8] - 2024-04-01
### Added
- Adds Normal map compression step

### Fixed
- Fixes not showing the window at launch if required steps are complete
- Fixes SDK update button to work with registry
- Fixes missing using directive when OpenXR is installed but not using the Android Build Target


## [2.0.7] - 2024-02-13
### Added
- Adds OpenXR and MLSDK support

## [2.0.6] - 2024-02-13
### Fixed
- Fixes minor bugs

## [2.0.5] - 2024-01-22
### Added
- Adds Unity Magic Leap Package dependency 
- Fixes check for required steps before auto opening window

## [2.0.4] - 2023-12-10
### Added
- Adds new logo style
- Adds window icon

## [2.0.3] - 2023-06-25
### Added
- Adds validation Step from Unity XR
- Adds Minimum API Step
- Adds Magic Leap Hub download instructions

## [2.0.2] - 2023-03-22
### Added
- Adds import as UPM Package

## [2.0.1] - 2023-03-20
### Added
- Adds the option to import Package with registry

### Fixed
- Fixes Typo

## [2.0.0] - 2023-03-17
### Added
- Adds support for Magic Leap 2
- Adds support for Unity 2022.x.x


## 1.1.0 - 2022-03-22
### Fixed
- Fixes bug that occationally prevented the Apply All function from completing.

## 1.1.0 - 2021-08-15
### Fixed
- Fixes a bug that prevented user's from building their application unless they manually removed the asset from their project.

### Added
- Adds import dialogue is now presented in a pretty window instead of the default dialogue box.
- Adds a window to all of the steps required to configure the Unity project based on Magic Leap's Getting Started guide.

## 1.0.1 - 2021-07-31
### Fixed
Fixes build error caused by MLSDKPackageImporter script getting included in non-editor targets.

## 1.0.0 - 2021-07-22
### Added
- Initial release