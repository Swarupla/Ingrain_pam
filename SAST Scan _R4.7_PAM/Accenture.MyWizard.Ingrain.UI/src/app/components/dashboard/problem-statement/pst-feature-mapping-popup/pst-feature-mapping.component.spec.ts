import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { PstFeatureMappingComponent } from './pst-feature-mapping.component';

describe('PstFeatureMappingComponent', () => {
  let component: PstFeatureMappingComponent;
  let fixture: ComponentFixture<PstFeatureMappingComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ PstFeatureMappingComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(PstFeatureMappingComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
