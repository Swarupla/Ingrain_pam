import { TestBed } from '@angular/core/testing';

import { CascadeModelsService } from './cascade-models.service';

describe('CascadeModelsService', () => {
  beforeEach(() => TestBed.configureTestingModule({}));

  it('should be created', () => {
    const service: CascadeModelsService = TestBed.get(CascadeModelsService);
    expect(service).toBeTruthy();
  });
});
