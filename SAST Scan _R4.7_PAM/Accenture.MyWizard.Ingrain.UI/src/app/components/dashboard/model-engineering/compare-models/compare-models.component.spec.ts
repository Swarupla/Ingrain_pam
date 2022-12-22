import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { CompareModelsComponent } from './compare-models.component';

describe('CompareModelsComponent', () => {
  let component: CompareModelsComponent;
  let fixture: ComponentFixture<CompareModelsComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ CompareModelsComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(CompareModelsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
