import { TestBed } from '@angular/core/testing';

import { DataEngineeringService } from './data-engineering.service';

describe('DataEngineeringService', () => {
  beforeEach(() => TestBed.configureTestingModule({}));

  it('should be created', () => {
    const service: DataEngineeringService = TestBed.get(DataEngineeringService);
    expect(service).toBeTruthy();
  });
});
