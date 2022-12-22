import { TestBed } from '@angular/core/testing';

import { CoreUtilsService } from './core-utils.service';

describe('CoreUtilsService', () => {
  beforeEach(() => TestBed.configureTestingModule({}));

  it('should be created', () => {
    const service: CoreUtilsService = TestBed.get(CoreUtilsService);
    expect(service).toBeTruthy();
  });
});
