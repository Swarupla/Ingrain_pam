import { TestBed } from '@angular/core/testing';

import { ProcessedDataService } from './processed-data.service';

describe('ContactService', () => {
  beforeEach(() => TestBed.configureTestingModule({}));

  it('should be created', () => {
    const service: ProcessedDataService = TestBed.get(ProcessedDataService);
    expect(service).toBeTruthy();
  });
});
