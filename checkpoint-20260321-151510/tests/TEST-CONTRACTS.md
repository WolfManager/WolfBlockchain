# WolfBlockchain Test Contracts

## Unit contracts

### Core validation and determinism
- `TransactionEnvelope_ShouldKeepVersionedDataExplicit`
- `ValidatorContracts_ShouldExposeDeterministicSignatures`
- `StateTransitionContext_ShouldBeExplicit`
- `BlockValidatorReturnsSameResultForSameInput`
- `MempoolRejectsDuplicateTransaction`
- `BlockchainEngineAcceptsSequentialBlock`

### Economics invariants
- `FeeCalculationResult_ShouldKeepAllFeeComponentsExplicit`
- `RewardAllocation_ShouldRequireReasonForAuditability`
- `SupplyPolicy_ShouldExposeIssuanceAndBurnContracts`
- `FeePolicyMaintainsFeeSumInvariant`
- `SupplyPolicyIssuanceAndBurnAreDeterministic`
- `RewardPolicyDistributesCollectedFeesToProposer`

### Wallet and security boundaries
- `WalletSignerDoesNotExposePrivateKeyMaterial`
- `ExternalMessageValidatorRejectsMalformedInput`

### Agent policy contracts
- `AgentActionRequest_ShouldKeepActionTypeExplicit`
- `AgentPolicyEvaluator_ShouldReturnValueTaskBoolean`
- `AgentActionResult_ShouldExposeExplicitOutcome`
- `PolicyEngineDeniesUnapprovedTransactionSubmission`
- `OrchestratorAllowsAuthorizedGatewayAction`

## Integration contracts

### Core-Consensus and Networking-Core
- `ConsensusEngineCommitsValidProposalFromCoreValidator`
- `NetworkingRouterDropsInvalidExternalMessageBeforeHandler`
- `ConsensusHandler_And_NetworkRouter_ShouldSharePeerMessageEnvelope`

### Core-Storage, Wallet-Api, Agents-Policy and Admin boundaries
- `CoreStorageBoundaryPersistsAcceptedBlockData`
- `WalletApiBoundarySubmitsSignedPayloadToMempool`
- `AgentsPolicyBoundaryDeniesUnsafeSubmitWithoutAuthorizationMarker`
- `AdminApiBoundaryRequiresRoleHeaderAuthorization`
- `PublicApiService_ShouldExposeExplicitApiResultContracts`
- `StorageRepositories_ShouldExposeCancellationTokenInAsyncMethods`
