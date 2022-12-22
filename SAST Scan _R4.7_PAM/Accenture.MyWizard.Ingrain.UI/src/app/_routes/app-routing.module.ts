import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { NotFoundComponent } from '../components/not-found/not-found.component';
import { ProcessedDataComponent } from '../components/processed-data/processed-data.component';
import { DashboardComponent } from '../components/dashboard/dashboard.component';
import { NeedAuthGuard } from '../_guards/auth.guard';
import { ProblemStatementComponent } from '../components/dashboard/problem-statement/problem-statement.component';
import { DataEngineeringComponent } from '../components/dashboard/data-engineering/data-engineering.component';
import { ModelEngineeringComponent } from '../components/dashboard/model-engineering/model-engineering.component';
import { DeployModelComponent } from '../components/dashboard/deploy-model/deploy-model.component';
import { PstTemplatesComponent } from '../components/dashboard/problem-statement/pst-templates/pst-templates.component';
import {
  PstUseCaseDefinitionComponent
} from '../components/dashboard/problem-statement/pst-use-case-definition/pst-use-case-definition.component';
import { HomeComponent } from '../components/home/home.component';
import { DataCleanupComponent } from '../components/dashboard/data-engineering/data-cleanup/data-cleanup.component';
import { PreprocessDataComponent } from '../components/dashboard/data-engineering/preprocess-data/preprocess-data.component';
import { FeatureSelectionComponent } from '../components/dashboard/model-engineering/feature-selection-model/feature-selection.component';
import { RecommendedAIComponent } from '../components/dashboard/model-engineering/recommended-ai/recommended-ai.component';
import { TeachAndTestComponent } from '../components/dashboard/model-engineering/teach-and-test/teach-and-test.component';
import { CompareModelsComponent } from '../components/dashboard/model-engineering/compare-models/compare-models.component';
import { TabauthGuard } from '../_guards/tabauth.guard';
import { PublishModelComponent } from '../components/dashboard/deploy-model/publish-model/publish-model.component';
import { DeployedModelComponent } from '../components/dashboard/deploy-model/deployed-end-model/deployed-model.component';
import { FocusAreaComponent } from '../components/dashboard/focus-area/focus-area.component';
import { HyperTuningComponent } from '../components/dashboard/model-engineering/teach-and-test/hyper-tuning/hyper-tuning.component';
import {
  WhatIfAnalysisComponent
} from '../components/dashboard/model-engineering/teach-and-test/what-if-analysis/what-if-analysis.component';
import { NlpServicesComponent } from '../components/dashboard/nlp-services/nlp-services.component';

// Monte Carlo Components
import { GridTableComponent } from '../montecarlo/ADSP/grid-table/grid-table.component';
import { FileUploadMappingComponent } from '../montecarlo/ADSP/file-upload-mapping/file-upload-mapping.component';
import { AdspLandingComponent } from '../montecarlo/ADSP/adsp-landing/adsp-landing.component';
import { InputSimulationComponent } from '../montecarlo/ADSP/input-simulation/input-simulation.component';
import { OutputSimulationComponent } from '../montecarlo/ADSP/output-simulation/output-simulation.component';
import { GenericLandingComponent } from '../montecarlo/Generic/generic-landing/generic-landing.component';
import { GenericUploadComponent } from '../montecarlo/Generic/generic-upload/generic-upload.component';
import { GenericInputComponent } from '../montecarlo/Generic/generic-input/generic-input.component';
import { GenericOutputComponent } from '../montecarlo/Generic/generic-output/generic-output.component';
import { ModelMonitoringComponent } from '../components/dashboard/deploy-model/model-monitoring/model-monitoring.component';
import { PredictionVisualization } from '../components/dashboard/deploy-model/prediction-visualization/prediction-visualization.component';

// Cascade models Components
import { CascadeModelsComponent } from '../components/dashboard/problem-statement/cascade-models/cascade-models.component';
import { ChooseCascadeModelsComponent } from '../components/dashboard/problem-statement/choose-cascade-models/choose-cascade-models.component';
import { MapCascadeModelsComponent } from '../components/dashboard/problem-statement/map-cascade-models/map-cascade-models.component';
import { TeachTestCascadeModelsComponent } from '../components/dashboard/problem-statement/teach-test-cascade-models/teach-test-cascade-models.component';
import { PublishCascadeModelsComponent } from '../components/dashboard/problem-statement/publish-cascade-models/publish-cascade-models.component';
import { DeployCascadeModelsComponent } from '../components/dashboard/problem-statement/deploy-cascade-models/deploy-cascade-models.component';
import { MyUtilitiesComponent } from '../components/dashboard/problem-statement/my-utilities/my-utilities.component';

import { AssetsTrackingComponent } from '../components/assets-usage/assets-tracking/assets-tracking.component';
 import { ShowVideoComponent } from '../components/header/show-video/show-video.component';
import { UploadDataComponent } from '../cascadeVisualization/upload-data/upload-data.component';
import { CascadeVisualizationGraphComponent } from '../cascadeVisualization/graphs/cascade-visualization-graph/cascade-visualization-graph.component';
// import { FmUploadDataComponent } from '../FMVisualization/fm-upload-data/fm-upload-data.component';
import { FmVisualizationGraphComponent } from '../FMVisualization/graphs/fm-visualization-graph/fm-visualization-graph.component';
import { InferenceEngineComponent } from '../inferenceEngine/inference-engine/inference-engine.component';
import { InferenceModelsComponent } from '../inferenceEngine/inference-models/inference-models.component';
import { IeConfigurationComponent } from '../inferenceEngine/ie-configuration/ie-configuration.component';
import { MarketplaceFileEncryptionComponent } from '../components/marketplace-file-encryption/marketplace-file-encryption.component';
import { ConfigureCertificationFlagComponent } from '../components/configure-certification-flag/configure-certification-flag.component';
import { LoginComponent } from '../components/login/login.component';
import { AdDashboardComponent } from '../components/anomaly-detection/ad-dashboard/ad-dashboard.component';
import { AdModelEngineeringComponent } from '../components/anomaly-detection/ad-model-engineering/ad-model-engineering.component';
import { AdPublishModelComponent } from '../components/anomaly-detection/ad-deploy-model/ad-publish-model/ad-publish-model.component';
import { AdDeployModelComponent } from '../components/anomaly-detection/ad-deploy-model/ad-deploy-model.component';
import { AdDeployedModelComponent } from '../components/anomaly-detection/ad-deploy-model/ad-deployed-model/ad-deployed-model.component';
import { AdUsecaseDefinitionComponent } from '../components/anomaly-detection/ad-usecase-definition/ad-usecase-definition.component';
import { AnomalyDetectionComponent } from '../components/anomaly-detection/anomaly-detection.component';

const appRoutes: Routes = [
  {path: 'login', component: LoginComponent},
  { path: 'processedData', component: ProcessedDataComponent },
  { path: 'landingPage', component: HomeComponent , canActivate: [NeedAuthGuard]},
  { path: 'assetsUsage', component: AssetsTrackingComponent },
  { path: 'MarketplaceFileEncryption', component: MarketplaceFileEncryptionComponent },
  { path: 'video', component: ShowVideoComponent },
  { path: 'notFound', component: NotFoundComponent },
  { path: 'choosefocusarea', component: FocusAreaComponent, canActivate: [NeedAuthGuard] },
  { path: 'reusable-NLP-services', component: NlpServicesComponent, canActivate: [NeedAuthGuard] },
  { path: 'ConfigureCertificationFlag', component: ConfigureCertificationFlagComponent },

  {
    path: 'dashboard',
    component: DashboardComponent,
    canActivate: [NeedAuthGuard],
    children: [
      // { path: '', redirectTo: 'choosefocusarea', pathMatch: 'full', canActivate: [NeedAuthGuard], },
      // { path: 'choosefocusarea', component: FocusAreaComponent, canActivate: [NeedAuthGuard] },
      // { path: 'reusable-NLP-services', component: NlpServicesComponent, canActivate: [NeedAuthGuard] },
      {
        path: 'problemstatement', component: ProblemStatementComponent, canActivate: [NeedAuthGuard],
        children: [
          { path: '', redirectTo: 'templates', pathMatch: 'full' },
          { path: 'templates', component: PstTemplatesComponent, canActivate: [NeedAuthGuard] },
          { path: 'utilities', component: MyUtilitiesComponent, canActivate: [NeedAuthGuard] },
          {
            path: 'cascadeModels', component: CascadeModelsComponent, canActivate: [NeedAuthGuard],
            children: [
              { path: '', redirectTo: 'chooseCascadeModels', pathMatch: 'full' },
              { path: 'chooseCascadeModels', component: ChooseCascadeModelsComponent, canActivate: [NeedAuthGuard] },
              { path: 'mapCascadeModels', component: MapCascadeModelsComponent, canActivate: [NeedAuthGuard] },
              //   { path: 'teachTestCascadeModels', component: TeachTestCascadeModelsComponent, canActivate: [NeedAuthGuard] },
              { path: 'publishCascadeModels', component: PublishCascadeModelsComponent, canActivate: [NeedAuthGuard] },
              { path: 'deployCascadeModels', component: DeployCascadeModelsComponent, canActivate: [NeedAuthGuard] }
            ]
          },
          { path: 'usecasedefinition', component: PstUseCaseDefinitionComponent, canActivate: [NeedAuthGuard] }
        ]
      },
      {
        path: 'dataengineering', component: DataEngineeringComponent, canActivate: [NeedAuthGuard, TabauthGuard],
        children: [
          { path: '', redirectTo: 'datacleanup', pathMatch: 'full' },
          { path: 'dataclaanup', component: DataCleanupComponent, canActivate: [NeedAuthGuard] },
          { path: 'datacleanup', component: DataCleanupComponent, canActivate: [NeedAuthGuard] },
          { path: 'preprocessdata', component: PreprocessDataComponent, canActivate: [NeedAuthGuard] },
        ]
      },
      {
        path: 'modelengineering', component: ModelEngineeringComponent, canActivate: [NeedAuthGuard, TabauthGuard],
        children: [
          { path: '', redirectTo: 'FeatureSelection', pathMatch: 'full' },
          { path: 'FeatureSelection', component: FeatureSelectionComponent, canActivate: [NeedAuthGuard] },
          { path: 'RecommendedAI', component: RecommendedAIComponent, canActivate: [NeedAuthGuard] },
          {
            path: 'TeachAndTest',
            component: TeachAndTestComponent,
            canActivate: [NeedAuthGuard],
            children: [
              { path: '', redirectTo: 'WhatIfAnalysis', pathMatch: 'full' },
              { path: 'WhatIfAnalysis', component: WhatIfAnalysisComponent, canActivate: [NeedAuthGuard] },
              { path: 'HyperTuning', component: HyperTuningComponent, canActivate: [NeedAuthGuard] }
            ],
          },
          { path: 'CompareModels', component: CompareModelsComponent, canActivate: [NeedAuthGuard] },

        ]
      },
      {
        path: 'deploymodel', component: DeployModelComponent, canActivate: [NeedAuthGuard, TabauthGuard],
        children: [
          { path: '', redirectTo: 'publishmodel', pathMatch: 'full' },
          { path: 'publishmodel', component: PublishModelComponent, canActivate: [NeedAuthGuard] },
          { path: 'deployedmodel', component: DeployedModelComponent, canActivate: [NeedAuthGuard] },
          { path: 'Monitoring', component: ModelMonitoringComponent, canActivate: [NeedAuthGuard] },
          { path: 'Prediction', component: PredictionVisualization, canActivate: [NeedAuthGuard] },
          { path: 'end', component: HomeComponent }
        ]
      }
    ]
  },
  // MonteCarlo Path
  // { path: 'Output', component: OutputSimulationComponent },
  {
    path: 'RiskReleasePredictor', component: AdspLandingComponent,
    children: [
      { path: '', redirectTo: 'upload', pathMatch: 'full' },
      { path: 'upload', component: FileUploadMappingComponent },
      { path: 'Input', component: InputSimulationComponent },
      { path: 'Output', component: OutputSimulationComponent }
    ]
  },
  {
    path: 'generic', component: GenericLandingComponent, canActivate: [NeedAuthGuard],
    children: [
      { path: '', redirectTo: 'upload', pathMatch: 'full' },
      { path: 'upload', component: GenericUploadComponent, canActivate: [NeedAuthGuard] },
      { path: 'Input', component: GenericInputComponent },
      { path: 'Output', component: GenericOutputComponent }
    ]
  },
  // Cascade Visualization
  {
    path: 'cascadeVisualization', component: UploadDataComponent, canActivate: [NeedAuthGuard]
  },
  {
    path: 'visualizationGraph', component: CascadeVisualizationGraphComponent
  },
  // FM Visualization
  // {
  //   path: 'fm-upload-data', component: FmUploadDataComponent
  // },
  {
    path: 'success-probability-visualization', component: FmVisualizationGraphComponent
  },
  { path: 'inferenceEngine', component: InferenceEngineComponent, canActivate: [NeedAuthGuard]},
  { path: 'ieConfigurationDetails', component: IeConfigurationComponent },
  // Anomaly Detection Starts
  {path :'anomaly-detection-dashboard', component: AdDashboardComponent},
  {
    path: 'anomaly-detection',
    component: AnomalyDetectionComponent,
    canActivate: [NeedAuthGuard],
    children: [
      // { path: '', redirectTo: 'choosefocusarea', pathMatch: 'full', canActivate: [NeedAuthGuard], },
      // { path: 'choosefocusarea', component: FocusAreaComponent, canActivate: [NeedAuthGuard] },
      // { path: 'reusable-NLP-services', component: NlpServicesComponent, canActivate: [NeedAuthGuard] },
      {
        path: 'problemstatement', component: ProblemStatementComponent, canActivate: [NeedAuthGuard],
        children: [
          { path: '', redirectTo: 'problemstatement', pathMatch: 'full' },
          //{ path: 'templates', component: PstTemplatesComponent, canActivate: [NeedAuthGuard] },
          { path: 'usecasedefinition', component: AdUsecaseDefinitionComponent, canActivate: [NeedAuthGuard] }
        ]
      },
      {
        path: 'modelengineering', component: ModelEngineeringComponent, canActivate: [NeedAuthGuard, TabauthGuard],
        children: [
          { path: '', redirectTo: 'FeatureSelection', pathMatch: 'full' },
          { path: 'FeatureSelection', component: AdModelEngineeringComponent, canActivate: [NeedAuthGuard] },
          // { path: 'RecommendedAI', component: RecommendedAIComponent, canActivate: [NeedAuthGuard] },
          // {
          //   path: 'TeachAndTest',
          //   component: TeachAndTestComponent,
          //   canActivate: [NeedAuthGuard],
          //   children: [
          //     { path: '', redirectTo: 'WhatIfAnalysis', pathMatch: 'full' },
          //     { path: 'WhatIfAnalysis', component: WhatIfAnalysisComponent, canActivate: [NeedAuthGuard] },
          //     { path: 'HyperTuning', component: HyperTuningComponent, canActivate: [NeedAuthGuard] }
          //   ],
          // },
          // { path: 'CompareModels', component: CompareModelsComponent, canActivate: [NeedAuthGuard] },

        ]
      },
      {
        path: 'deploymodel', component: AdDeployModelComponent, canActivate: [NeedAuthGuard, TabauthGuard],
        children: [
          { path: '', redirectTo: 'publishmodel', pathMatch: 'full' },
          { path: 'publishmodel', component: AdPublishModelComponent, canActivate: [NeedAuthGuard] },
          { path: 'deployedmodel', component: AdDeployedModelComponent, canActivate: [NeedAuthGuard] },
          // { path: 'Monitoring', component: ModelMonitoringComponent, canActivate: [NeedAuthGuard] },
          // { path: 'Prediction', component: PredictionVisualization, canActivate: [NeedAuthGuard] },
          { path: 'end', component: HomeComponent }
        ]
      }
    ]
  },
  // Anomaly Detection Ends
  { path: '', redirectTo: '/landingPage', pathMatch: 'full' },
  { path: '**', component: NotFoundComponent }
];
@NgModule({
  declarations: [],
  imports: [
    RouterModule.forRoot(appRoutes)
  ],
  exports: [RouterModule]
})


export class AppRoutingModule { }
