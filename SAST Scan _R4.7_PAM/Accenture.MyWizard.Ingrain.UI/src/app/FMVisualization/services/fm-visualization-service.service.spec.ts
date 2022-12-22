import { TestBed } from '@angular/core/testing';

import { FmVisualizationServiceService } from './fm-visualization-service.service';

describe('FmVisualizationServiceService', () => {
  beforeEach(() => TestBed.configureTestingModule({}));

  it('should be created', () => {
    const service: FmVisualizationServiceService = TestBed.get(FmVisualizationServiceService);
    expect(service).toBeTruthy();
  });
});
