import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { ViewDatasetrecordsComponent } from './view-datasetrecords.component';

describe('ViewDatasetrecordsComponent', () => {
  let component: ViewDatasetrecordsComponent;
  let fixture: ComponentFixture<ViewDatasetrecordsComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ ViewDatasetrecordsComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(ViewDatasetrecordsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
