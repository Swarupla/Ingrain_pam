import { TestBed } from '@angular/core/testing';

import { CompareModelsService } from './compare-models.service';

describe('CompareModelsService', () => {
  beforeEach(() => TestBed.configureTestingModule({}));

  it('should be created', () => {
    const service: CompareModelsService = TestBed.get(CompareModelsService);
    expect(service).toBeTruthy();
  });
});
