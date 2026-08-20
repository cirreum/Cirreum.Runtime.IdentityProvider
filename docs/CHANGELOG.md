# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Updated

- Updated NuGet packages.

## [1.1.3] - 2026-08-04

### Updated

- Updated NuGet packages.

## [1.1.2] - 2026-07-29

### Updated

- Updated NuGet packages.

## [1.1.1] - 2026-07-23

### Updated

- Updated NuGet packages.

## [1.1.0] - 2026-07-20

### Added

- Orphaned-provisioner detection (ADR-0030): `AddProvisioner<T>(instanceKey)` now
  verifies its instance key against the configured identity provider instances at
  composition time. A key matching no instance emits a deferred **Warning**
  (fail-fast at startup validation) naming the known instance keys and the expected
  configuration shape — previously this misconfiguration surfaced only as a silent
  404 on the provisioning callback route. A key matching only disabled instances
  emits an **Information** advisory (legitimate per-environment disabling).
- `RegisterIdentityProvider<,,>()` records every configured instance (enabled or
  not) into the service collection as the data source for the check.

## [1.0.6] - 2026-07-19

### Updated

- Updated NuGet packages.

## [1.0.5] - 2026-07-04

### Updated
- Updated NuGet packages.

## [1.0.3] - 2026-05-01

### Updated
- Updated NuGet packages.

